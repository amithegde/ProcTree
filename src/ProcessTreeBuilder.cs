using System.Runtime.InteropServices;

namespace ProcTree
{
    public class ProcessTreeBuilder
    {
        private readonly string processName;
        private readonly TreeLogger logger;
        private readonly Dictionary<int, ProcessInfo> processDict = new Dictionary<int, ProcessInfo>();
        private readonly Dictionary<int, List<int>> parentToChildren = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, string> userNameCache = new Dictionary<int, string>();

        public ProcessTreeBuilder(string processName, TreeLogger logger)
        {
            this.processName = processName;
            this.logger = logger;
        }

        public void BuildAndPrintProcessTrees()
        {
            BuildProcessDictionaries();

            // Find root processes with the specified name
            var roots = new List<int>();
            foreach (var kvp in processDict)
            {
                var proc = kvp.Value;
                if (string.Equals(proc.Name, processName, StringComparison.OrdinalIgnoreCase))
                {
                    roots.Add(proc.ProcessId);
                }
            }

            if (roots.Count == 0)
            {
                Console.WriteLine($"Process '{processName}' not found.");
                return;
            }

            var visited = new HashSet<int>();
            foreach (var rootPid in roots)
            {
                // Clear visited set for each root to handle multiple trees separately
                visited.Clear();
                // Print a separator between trees if there are multiple roots
                if (roots.Count > 1)
                {
                    Console.WriteLine(new string('-', 50));
                }
                PrintProcessTree(rootPid, "", true, visited, isRoot: true);
            }
        }

        private void BuildProcessDictionaries()
        {
            IntPtr hSnapshot = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.TH32CS_SNAPPROCESS, 0);
            if (hSnapshot == IntPtr.Zero || hSnapshot == (IntPtr)(-1))
            {
                Console.WriteLine("Failed to create snapshot.");
                return;
            }

            var pe32 = new NativeMethods.PROCESSENTRY32();
            pe32.dwSize = (uint)Marshal.SizeOf(typeof(NativeMethods.PROCESSENTRY32));

            if (NativeMethods.Process32First(hSnapshot, ref pe32))
            {
                do
                {
                    int processId = (int)pe32.th32ProcessID;
                    int parentProcessId = (int)pe32.th32ParentProcessID;
                    string name = pe32.szExeFile;

                    var processInfo = new ProcessInfo()
                    {
                        ProcessId = processId,
                        ParentProcessId = parentProcessId,
                        Name = name,
                    };

                    processDict[processId] = processInfo;

                    if (!parentToChildren.ContainsKey(parentProcessId))
                    {
                        parentToChildren[parentProcessId] = new List<int>();
                    }
                    parentToChildren[parentProcessId].Add(processId);

                } while (NativeMethods.Process32Next(hSnapshot, ref pe32));
            }
            else
            {
                Console.WriteLine("Failed to retrieve process information.");
            }

            NativeMethods.CloseHandle(hSnapshot);
        }

        private void PrintProcessTree(int processId, string indent, bool isLast, HashSet<int> visited, bool isRoot = false)
        {
            if (visited.Contains(processId))
                return;

            if (!processDict.TryGetValue(processId, out var processInfo))
                return;

            visited.Add(processId);

            // Get start time efficiently
            string startTimeStr = "N/A";
            var startTime = GetProcessStartTime(processId);
            if (startTime.HasValue)
            {
                startTimeStr = GetElapsedTime(startTime.Value);
            }

            // Get user name
            string userName = "N/A";
            var uName = GetProcessUserName(processId);
            if (!string.IsNullOrEmpty(uName))
            {
                userName = uName;
            }

            // Print the current process
            logger.PrintProcessLine(indent, isLast, isRoot, processInfo, startTimeStr, userName);

            // Update indent for child processes
            string childIndent = indent;
            if (!isRoot)
            {
                childIndent += isLast ? "   " : "│  ";
            }

            // Get child processes
            if (parentToChildren.TryGetValue(processId, out var children))
            {
                // For each child process
                for (int i = 0; i < children.Count; i++)
                {
                    var childPid = children[i];
                    bool isLastChild = (i == children.Count - 1);
                    PrintProcessTree(childPid, childIndent, isLastChild, visited);
                }
            }
        }

        private DateTime? GetProcessStartTime(int processId)
        {
            // First attempt using OpenProcess and GetProcessTimes
            IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryLimitedInformation, false, processId);
            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    if (NativeMethods.GetProcessTimes(hProcess, out NativeMethods.FILETIME ftCreation, out _, out _, out _))
                    {
                        long fileTime = ((long)ftCreation.dwHighDateTime << 32) + ftCreation.dwLowDateTime;
                        return DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
                    }
                }
                finally
                {
                    NativeMethods.CloseHandle(hProcess);
                }
            }
            else
            {
                // OpenProcess failed, attempt to use Process.GetProcessById as a fallback
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(processId);
                    return process.StartTime;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Access denied
                    // Optionally log the exception or handle it as needed
                }
                catch (ArgumentException)
                {
                    // Process not found
                }
                catch (Exception)
                {
                    // Other exceptions
                    // Optionally log the exception or handle it as needed
                }
            }

            // If all methods fail, return null
            return null;
        }

        private string GetProcessUserName(int processId)
        {
            if (userNameCache.TryGetValue(processId, out string cachedUserName))
            {
                return cachedUserName;
            }

            try
            {
                IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryLimitedInformation, false, processId);
                if (hProcess == IntPtr.Zero)
                    return null;
                try
                {
                    IntPtr hToken;
                    if (!NativeMethods.OpenProcessToken(hProcess, NativeMethods.TOKEN_QUERY, out hToken))
                    {
                        return null;
                    }
                    try
                    {
                        uint tokenInfoLength = 0;
                        NativeMethods.GetTokenInformation(hToken, NativeMethods.TOKEN_INFORMATION_CLASS.TokenUser, IntPtr.Zero, 0, out tokenInfoLength);
                        if (tokenInfoLength == 0)
                            return null;

                        IntPtr tokenInfo = Marshal.AllocHGlobal((int)tokenInfoLength);
                        try
                        {
                            if (NativeMethods.GetTokenInformation(hToken, NativeMethods.TOKEN_INFORMATION_CLASS.TokenUser, tokenInfo, tokenInfoLength, out tokenInfoLength))
                            {
                                var tokenUser = (NativeMethods.TOKEN_USER)Marshal.PtrToStructure(tokenInfo, typeof(NativeMethods.TOKEN_USER));
                                IntPtr pSid = tokenUser.User.Sid;

                                uint cchName = 0;
                                uint cchReferencedDomainName = 0;
                                NativeMethods.SID_NAME_USE sidUse;

                                NativeMethods.LookupAccountSid(null, pSid, null, ref cchName, null, ref cchReferencedDomainName, out sidUse);

                                if (Marshal.GetLastWin32Error() == 122) // ERROR_INSUFFICIENT_BUFFER
                                {
                                    var name = new System.Text.StringBuilder((int)cchName);
                                    var domainName = new System.Text.StringBuilder((int)cchReferencedDomainName);

                                    if (NativeMethods.LookupAccountSid(null, pSid, name, ref cchName, domainName, ref cchReferencedDomainName, out sidUse))
                                    {
                                        string userName = domainName.ToString() + "\\" + name.ToString();
                                        userNameCache[processId] = userName;
                                        return userName;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(tokenInfo);
                        }
                    }
                    finally
                    {
                        NativeMethods.CloseHandle(hToken);
                    }
                }
                finally
                {
                    NativeMethods.CloseHandle(hProcess);
                }
            }
            catch
            {
                // Ignore exceptions
            }
            return null;
        }

        private string GetElapsedTime(DateTime startTime)
        {
            var elapsed = DateTime.Now - startTime;
            return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m {elapsed.Seconds}s ago";
        }
    }
}
