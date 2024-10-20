# ProcTree

Displays process tree of a process on the console. Prints each tree node as soon as all details for the node are gathered instead of loading complete tree first and printing the tree at the end. Uses native API calls instead of WMI to be efficient.

## Usage

`ProcTree <processName>`

E.g.: `ProcTree services`

![alt text](snap.png)
