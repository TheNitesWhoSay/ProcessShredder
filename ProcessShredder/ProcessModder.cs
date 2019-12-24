using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection.Emit;

/// <summary>
/// A namespace with mechanisms for interacting
/// with processes at a low level
/// 
/// By Justin Forsberg
/// </summary>
namespace ProcessModder
{
    /// <summary>
    /// A class for hooking and interacting with processes,
    /// includes (but is not limited to) methods for manipulating
    /// the memory of, freezing/unfreezing, and terminating processes.
    /// </summary>
    class ProcessMod : IDisposable
    {
        // Data

        /// <summary>Whether this program has the SE_DEBUG_NAME privilege</summary>
        private static bool haveDebugPrivileges = false;
        /// <summary>The handle for a hooked process, may be NULL</summary>
        private uint hookedProcess;
        /// <summary>Whether a debugger has been attached to the process</summary>
        private bool debugging;

        // Constructors/Cleanup

        /// <summary>Constructs a ProcessMod object with no hooked process</summary>
        public ProcessMod()
        {
            hookedProcess = NULL;
            debugging = false;
        }
        /// <summary>Ensures that unmanaged objects are cleaned up</summary>
        public void Dispose()
        {
            close();
        }

        // Attempts to find and hook the process with the given information

        /// <summary>
        /// Attempts to hook the process with maximumAccess, minimumAccess
        /// with WRITE_DAC or at least minimumAccess
        /// </summary>
        /// <param name="processID">The ID of the process to be hooked</param>
        /// <param name="minimumAccess">The minimum access to open with</param>
        /// <param name="maximumAccess">The maximum/desired access to open with</param>
        /// <returns>Whether the process was hooked with at least minimumAccess</returns>
        public bool openWithProcessID(uint processID, uint minimumAccess, uint maximumAccess)
        {
            return Hook(processID, minimumAccess, maximumAccess);
        }

        /// <summary>
        /// Attempts to hook the process, with a given accessLevel
        /// </summary>
        /// <param name="processID">The ID of the process to be hooked</param>
        /// <param name="accessLevel">The access level to hook with</param>
        /// <returns>Whether the process was hooked successfully</returns>
        public bool openWithProcessID(uint processID, uint accessLevel)
        {
            return openWithProcessID(processID, accessLevel, accessLevel);
        }

        /// <summary>Attempts to hook the process with maximal access</summary>
        /// <param name="processID">The ID of the process to be hooked</param>
        /// <returns>Whether the process was hooked successfully</returns>
        public bool openWithProcessID(uint processID)
        {
            return openWithProcessID(processID, PROCESS_ALL_LEGACY_ACCESS, PROCESS_ALL_ACCESS);
        }

        /// <summary>
        /// Attempts to hook the process with maximumAccess, minimumAccess
        /// with WRITE_DAC or at least minimumAccess
        /// </summary>
        /// <param name="processName">The name of the process to be hooked</param>
        /// <param name="minimumAccess">The minimum access to open with</param>
        /// <param name="maximumAccess">The maximum/desired access to open with</param>
        /// <returns>Whether the process was hooked with at least minimumAccess</returns>
        public bool openWithProcessName(string processName, uint minimumAccess, uint maximumAccess)
        {
            uint processID = 0;

            return FindProcess(processName, ref processID) &&
                Hook(processID, minimumAccess, maximumAccess);
        }

        /// <summary>
        /// Attempts to hook the process, with a given accessLevel
        /// </summary>
        /// <param name="processName">The name of the process to be hooked</param>
        /// <param name="accessLevel">The access level to hook with</param>
        /// <returns>Whether the process was hooked successfully</returns>
        public bool openWithProcessName(string processName, uint accessLevel)
        {
            return openWithProcessName(processName, accessLevel, accessLevel);
        }

        /// <summary>Attempts to hook the process with maximal access</summary>
        /// <param name="processName">The name of the process to be hooked</param>
        /// <returns>Whether the process was hooked successfully</returns>
        public bool openWithProcessName(string processName)
        {
            return openWithProcessName(processName, PROCESS_ALL_LEGACY_ACCESS, PROCESS_ALL_ACCESS);
        }

        /// <summary>
        /// Attempts to hook the process with maximumAccess, minimumAccess
        /// with WRITE_DAC or at least minimumAccess
        /// </summary>
        /// <param name="windowName">The name of the window of the process to be hooked</param>
        /// <param name="minimumAccess">The minimum access to open with</param>
        /// <param name="maximumAccess">The maximum/desired access to open with</param>
        /// <returns>Whether the process was hooked with at least minimumAccess</returns>
        public unsafe bool openWithWindowName(string windowName, uint minimumAccess, uint maximumAccess)
        {
            uint processID = 0;

            FixedWideString str;
            if ( windowName.Length >= 260 )
                return false;

            for ( int i = 0; i < windowName.Length; i++ )
                str.str[i] = windowName[i];
            str.str[windowName.Length] = '\0';

            uint hWindow = FindWindowW(0, (uint)(&(str.str[0])));

            return hWindow != NULL &&
                   GetWindowThreadProcessId(hWindow, (uint)(&processID)) != 0 &&
                   processID != 0 &&
                   Hook(processID, minimumAccess, maximumAccess);
        }

        /// <summary>
        /// Attempts to hook the process, with a given accessLevel
        /// </summary>
        /// <param name="windowName">The name of the window of the process to be hooked</param>
        /// <param name="accessLevel">The access level to hook with</param>
        /// <returns>Whether the process was hooked successfully</returns>
        public unsafe bool openWithWindowName(string windowName, uint accessLevel)
        {
            return openWithWindowName(windowName, accessLevel, accessLevel);
        }

        /// <summary>Attempts to hook the process with maximal access</summary>
        /// <param name="windowName">The name of the window of the process to be hooked</param>
        /// <returns>Whether the process was hooked successfully</returns>
        public unsafe bool openWithWindowName(string windowName)
        {
            return openWithWindowName(windowName, PROCESS_ALL_LEGACY_ACCESS, PROCESS_ALL_ACCESS);
        }

        /// <summary>
        /// Attempts to hook the process with maximumAccess, minimumAccess
        /// with WRITE_DAC or at least minimumAccess
        /// </summary>
        /// <param name="hProcess">A handle to the process to be hooked</param>
        /// <param name="minimumAccess">The minimum access to open with</param>
        /// <param name="maximumAccess">The maximum/desired access to open with</param>
        /// <returns>Whether the process was hooked with at least minimumAccess</returns>
        public bool openWithProcessHandle(uint hProcess, uint minimumAccess, uint maximumAccess)
        {
            uint processID = GetProcessId(hProcess);

            return processID != 0 &&
                   Hook(processID, minimumAccess, maximumAccess);
        }

        /// <summary>
        /// Attempts to hook the process, with a given accessLevel
        /// </summary>
        /// <param name="hProcess">A handle to the process to be hooked</param>
        /// <param name="accessLevel">The access level to hook with</param>
        /// <returns>Whether the process was hooked successfully</returns>
        public bool openWithProcessHandle(uint hProcess, uint accessLevel)
        {
            return openWithProcessHandle(hProcess, accessLevel, accessLevel);
        }

        /// <summary>Attempts to hook the process with maximal access</summary>
        /// <param name="hProcess">A handle to the process to be hooked</param>
        /// <returns>Whether the process was hooked successfully</returns>
        public bool openWithProcessHandle(uint hProcess)
        {
            return openWithProcessHandle(hProcess, PROCESS_ALL_LEGACY_ACCESS, PROCESS_ALL_ACCESS);
        }

        /// <summary>Checks whether there is a hooked process that is still open</summary>
        /// <returns>Whether there is a hooked process that is still open</returns>
        public unsafe bool isOpen()
        {
            uint lpExitCode;

            if ( hookedProcess != NULL &&
                 GetExitCodeProcess(hookedProcess, (uint)(&lpExitCode)) != 0 &&
                 lpExitCode == STILL_ACTIVE )
            {
                return true;
            }
            else
            {
                hookedProcess = NULL;
                debugging = false;
                return false;
            }
        }

        /// <summary>
        /// Cleans up process hooks, stops debugging, and clears the handle
        /// </summary>
        public void close()
        {
            if ( isOpen() )
            {
                uint processId = GetProcessId(hookedProcess);
                if ( debugging && processId != 0 )
                    debugging = DebugActiveProcessStop(processId) != 0;

                CloseHandle(hookedProcess);
            }
            hookedProcess = NULL;
            debugging = false;
        }

        /// <summary>Determines whether the calling process is running with the SE_DEBUG_NAME privilege</summary>
        /// <returns>Whether the calling process is running with the SE_DEBUG_NAME privilege</returns>
        public static bool isDebugElevated()
        {
            return haveDebugPrivileges;
        }

        /// <summary>Writes 'value' to the given address</summary>
        /// <typeparam name="T">The type of value to write to memory</typeparam>
        /// <param name="address">The address in memory to write too</param>
        /// <param name="value">The value to write to the given address</param>
        /// <returns>Whether the value was written to memory successfully</returns>
        public unsafe bool writeMem<T>(uint address, T value)
        {
            bool success = false;
            uint valueSize = (uint)Sizes.SizeOf<T>();
            byte[] managedBuffer = null;
            if ( objectToBytes<T>(value, ref managedBuffer) )
            {
                byte* unmanagedBuffer = (byte*)HeapAlloc(heap, HEAP_ZERO_MEMORY, valueSize);
                if ( (uint)unmanagedBuffer != NULL )
                {
                    copyBuffer(managedBuffer, unmanagedBuffer, valueSize);
                    success = WriteProcessMemory(hookedProcess, address, unmanagedBuffer, valueSize, NULL) != 0;
                    HeapFree(heap, NULL, unmanagedBuffer);
                }
            }
            return success;
        }

        /// <summary>Reads the value at the given address</summary>
        /// <typeparam name="T">The type of the value to be read</typeparam>
        /// <param name="address">The address of the value to be read</param>
        /// <param name="value">Set to the read value on success</param>
        /// <returns>Whether the value at the given address was read</returns>
        public unsafe bool readMem<T>(uint address, ref T value)
        {
            bool success = false;
            uint valueSize = (uint)Sizes.SizeOf(value);
            byte* unmanagedBuffer = (byte*)HeapAlloc(heap, HEAP_ZERO_MEMORY, valueSize);
            if ( (uint)unmanagedBuffer != NULL )
            {
                if ( ReadProcessMemory(hookedProcess, address, unmanagedBuffer, valueSize, NULL) == TRUE )
                    success = bytesToObject(unmanagedBuffer, valueSize, ref value);

                HeapFree(heap, NULL, unmanagedBuffer);
            }
            return success;
        }

        /// <summary>Attempts to read a value from memory</summary>
        /// <typeparam name="T">The type of value to get</typeparam>
        /// <param name="address">The address of the value you're getting</param>
        /// <returns>The value read on success, default(T) otherwise</returns>
        public T val<T>(uint address)
        {
            T obj = default(T);
            readMem<T>(address, ref obj);
            return obj;
        }

        /// <summary>Attemps to find the address of a given array</summary>
        /// <param name="baseAddress">The address of the array on success, 0 otherwise</param>
        /// <param name="arrayItems">The array items to search for</param>
        /// <param name="arrayLength">The length of the array you're searching for</param>
        public bool GetArrayAddress(ref uint baseAddress, uint arrayItems, uint arrayLength) // TODO
        {
            return false;
        }

        /// <summary>Finds the base address of the hooked process</summary>
        /// <param name="address">Set to the base address of the process on success</param>
        /// <returns>true on success, false otherwise</returns>
        public bool GetBaseAddress(ref uint address) // TODO
        {
            return false;
        }

        /// <summary>Finds the amount of contiguous memory after startAddress</summary>
        /// <param name="startAddress">The starting point to examine memory at</param>
        /// <param name="size">Set to the size of the memory block on success</param>
        /// <returns>true on success, false otherwise</returns>
        public bool GetMemRegionSize(uint startAddress, ref uint size) // TODO
        {
            return false;
        }

        /// <summary>
        /// Prints all the threads for this process
        /// </summary>
        public void printThreads() // TODO
        {

        }

        /// <summary>
        /// Prints all system memory and the access this program has to it
        /// </summary>
        /// <param name="includeReadOnly">
        /// Whether memory regions with read access should be
        /// included rather than just those with read-write
        /// </param>
        public void printAccessibleMemRegions(bool includeReadOnly) // TODO
        {

        }

        /// <summary>
        /// Prints information from the provided memory region structure
        /// </summary>
        void PrintMemRegionInfo(/*MEMORY_BASIC_INFORMATION &region*/) // TODO
        {

        }

        /// <summary>Attempts to freeze the hooked process by attatching a debugger</summary>
        /// <returns>Whether the operation was successful</returns>
        public bool freezeProcess()
        {
            bool success = false;
            bool hadDebugPrivs = haveDebugPrivileges;
            if ( isOpen() && GetDebugPrivileges() )
            {
                uint processId = GetProcessId(hookedProcess);
                if ( processId != 0 )
                {
                    if ( DebugActiveProcess(processId) != 0 )
                    {
                        debugging = true;
                        success = true;
                    }
                    else if ( GetLastError() == ERROR_NOT_SUPPORTED )
                        Console.WriteLine("Cannot freeze 64bit applications with a 32bit process!");
                }

                if ( !hadDebugPrivs )
                    ReleaseDebugPrivileges();
            }
            return success;
        }

        /// <summary>
        /// Attempts to unfreeze the hooked process by detaching a debugger
        /// </summary>
        /// <returns>true if the hooked process isn't being debugged by this object, false otherwise</returns>
        public bool unfreezeProcess()
        {
            if ( isOpen() && debugging && DebugActiveProcessStop(GetProcessId(hookedProcess)) != 0 )
                debugging = false;

            return !debugging;
        }

        /// <summary>Forcefully terminates the hooked process</summary>
        /// <returns>Whether the hooked process was terminated</returns>
        public bool terminateProcess()
        {
            uint processId = GetProcessId(hookedProcess);
            if ( processId != 0 )
            {
                if ( debugging )
                    debugging = DebugActiveProcessStop(processId) != 0;

                return TerminateProcess(hookedProcess, 0) != 0;
            }
            return false;
        }

        /// <summary>Returns the handle of the hooked process</summary>
        /// <returns>
        /// The handle of the hooked process,
        /// or NULL if no process is hooked
        /// </returns>
        public uint getHandle()
        {
            return hookedProcess;
        }

        /// <summary>Gets a list of all processes running on the system</summary>
        /// <returns>
        /// A list of all processes running on the system
        /// on success, an empty list otherwise
        /// </returns>
        public static unsafe List<ProcessEntry> getProcessList()
        {
            List<ProcessEntry> processList = new List<ProcessEntry>();

            uint hProcessSnap = NULL;
            ProcessEntry32 pe32 = new ProcessEntry32(); // Holds information about a process
            pe32.dwSize = (uint)sizeof(ProcessEntry32);

            hProcessSnap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0); // Get list of running processes
            if ( hProcessSnap != INVALID_HANDLE_VALUE ) // Got list successfully
            {
                if ( Process32FirstW(hProcessSnap, (uint)(&pe32)) == TRUE ) // If you can get information about the first process
                {
                    do // Step through processes
                    {
                        ProcessEntry processEntry = new ProcessEntry();
                        processEntry.ProcessId = pe32.th32ProcessID;
                        processEntry.NumThreads = pe32.cntThreads;
                        processEntry.ParentProcessId = pe32.th32ParentProcessID;
                        processEntry.BasePriority = pe32.pcPriClassBase;
                        if ( pe32.szExeFile[0] != '\0' ) // Name is null?
                            processEntry.ExeFileName = new string(pe32.szExeFile);
                        else
                            processEntry.ExeFileName = "[Null]";

                        processList.Add(processEntry);
                    }
                    while ( Process32NextW(hProcessSnap, (uint)&pe32) == TRUE ); // While there's more processes
                }

                CloseHandle(hProcessSnap);
            }

            return processList;
        }

        /// <summary>Gets the Id of the calling process</summary>
        /// <returns>The Id of the calling process</returns>
        public static uint getCurrentProcessId()
        {
            return GetCurrentProcessId();
        }

        /// <summary>
        /// Gets the username associated with the given processId
        /// </summary>
        /// <param name="processId">The Id whose username you wish to find</param>
        /// <returns>The username assocated with the given processId</returns>
        public static unsafe string getUsername(uint processId)
        {
            if ( processId == 0 || processId == 4 )
                return "SYSTEM";

            string username = "UNKNOWN";
            using ( ProcessMod process = new ProcessMod() )
            {
                if ( process.openWithProcessID(processId, PROCESS_QUERY_INFORMATION, PROCESS_QUERY_LIMITED_INFORMATION) )
                {
                    uint tokenHandle = NULL;
                    if ( OpenProcessToken(process.getHandle(), TOKEN_QUERY, &tokenHandle) != 0 )
                    {
                        uint returnSize = 0, destSize = 0;
                        TokenUser* userStruct = (TokenUser*)NULL;
                        if ( GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser, userStruct,
                                0, &returnSize) == FALSE )
                        {
                            userStruct = (TokenUser*)HeapAlloc(heap, HEAP_ZERO_MEMORY, returnSize);
                            if ( (uint)userStruct != NULL )
                            {
                                if ( GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenUser,
                                        userStruct, returnSize, &destSize) != FALSE )
                                {
                                    SID_NAME_USE name;
                                    uint domainSize = 260;
                                    FixedWideString dest, domain;
                                    if ( LookupAccountSidW((char*)NULL, userStruct->User.Sid, dest.str, &destSize,
                                        domain.str, &domainSize, &name) != 0 )
                                    {
                                        username = new string(dest.str);
                                    }
                                }
                                HeapFree(GetProcessHeap(), 0, (byte*)userStruct);
                            }
                        }
                        CloseHandle(tokenHandle);
                    }
                    else
                    {
                        username = "PROTECTED";
                    }

                    process.close();
                }
            }
            return username;
        }

        /// <summary>Attempts to get elevated debug privileges for this process</summary>
        /// <returns>Whether elevated debug privileges were received</returns>
        public static unsafe bool GetDebugPrivileges()
        {
            if ( haveDebugPrivileges )
                return true;

            FixedWideString seDebugName;
            for ( int i = 0; i < SE_DEBUG_NAME.Length; i++ )
                seDebugName.str[i] = SE_DEBUG_NAME[i];
            seDebugName.str[SE_DEBUG_NAME.Length] = '\0';

            bool success = false;
            uint desiredAccess = TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY;
            uint hToken = 0;
            if ( OpenThreadToken(GetCurrentThread(), desiredAccess, TRUE, (uint)(&hToken)) != 0 ) // Try opening this threads token
            {
                success = SetPrivilege(hToken, seDebugName.str, TRUE);
                CloseHandle(hToken);
            }
            else if ( GetLastError() == ERROR_NO_TOKEN && // This thread had no token
                ImpersonateSelf(SecurityImpersonation) != 0 && // Create a token for this thread
                OpenThreadToken(GetCurrentThread(), desiredAccess, TRUE, (uint)(&hToken)) != 0 ) // Open the created token
            {
                success = SetPrivilege(hToken, seDebugName.str, TRUE); // Enable debug privileges
                CloseHandle(hToken);
            }
            haveDebugPrivileges = success;
            return success;
        }

        /// <summary>
        /// Releases any elevated debug privileges this process holds
        /// </summary>
        public static unsafe void ReleaseDebugPrivileges()
        {
            if ( !haveDebugPrivileges )
                return;

            FixedWideString seDebugName;
            for ( int i = 0; i < SE_DEBUG_NAME.Length; i++ )
                seDebugName.str[i] = SE_DEBUG_NAME[i];
            seDebugName.str[SE_DEBUG_NAME.Length] = '\0';

            uint hToken;
            uint desiredAccess = TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY;

            // Try opening this threads token
            if ( OpenThreadToken(GetCurrentThread(), desiredAccess, TRUE, (uint)(&hToken)) != 0 )
            {
                if ( SetPrivilege(hToken, seDebugName.str, FALSE) ) // Disable debug privileges
                    haveDebugPrivileges = false;

                CloseHandle(hToken); // Close the token
            }
        }

        /// <summary>Attempts to hook the process with at least minimumAccess</summary>
        /// <param name="processID">The ID of the process to be hooked</param>
        /// <param name="minimumAccess">The minimum access with which to hook the process</param>
        /// <param name="desiredAccess">The desired access level with which to hook the process</param>
        /// <returns>Whether the process was hooked with at least minimumAccess</returns>
        protected bool Hook(uint processID, uint minimumAccess, uint desiredAccess)
        {
            if ( isOpen() )
                close();

            desiredAccess |= minimumAccess;
            return HookProcess(processID, minimumAccess, desiredAccess) ||
                   HookWithDebugMod(processID, minimumAccess, desiredAccess) ||
                   HookWithDaclMod(processID, minimumAccess, desiredAccess);
        }

        /// <summary>Attempts to hook the process using elevated debug privileges</summary>
        /// <param name="processID">The ID of the process to be hooked</param>
        /// <param name="minimumAccess">The minimum access with which to hook the process</param>
        /// <param name="desiredAccess">The desired access level with which to hook the process</param>
        /// <returns>Whether the process was hooked with at least minimumAccess</returns>
        protected bool HookWithDebugMod(uint processID, uint minimumAccess, uint desiredAccess)
        {
            bool success = false;
            bool hadDebugPrivs = haveDebugPrivileges;
            if ( GetDebugPrivileges() )
            {
                // Guarantees desiredAccess for valid ids unless DRM protection is active
                success = HookProcess(processID, minimumAccess, desiredAccess);
                if ( !hadDebugPrivs )
                    ReleaseDebugPrivileges();
            }
            return success;
        }

        /// <summary>Attempts to hook the process using DACL access modifications</summary>
        /// <param name="processID">The ID of the process to be hooked</param>
        /// <param name="minimumAccess">The minimum access with which to hook the process</param>
        /// <param name="desiredAccess">The desired access level with which to hook the process</param>
        /// <returns>Whether the process was hooked with at least minimumAccess</returns>
        protected unsafe bool HookWithDaclMod(uint processID, uint minimumAccess, uint desiredAccess)
        {
            uint daclHook = OpenProcess(WRITE_DAC | READ_CONTROL, FALSE, processID); // Open process with DACL access
            bool haveReadControl = (daclHook != NULL);
            if ( daclHook == NULL )
                daclHook = OpenProcess(WRITE_DAC, FALSE, processID); // Open with just write access

            if ( daclHook != NULL ) // Successfully gained DACL access
            {
                ACL* daclPrev = (ACL*)NULL, // Pointers to access information (ie: whether you can read the process's memory)
                     daclReplace = (ACL*)NULL;

                // Store the existing DACL information, if it's usable
                bool prevStored = haveReadControl &&
                    (desiredAccess & WRITE_DAC) == WRITE_DAC &&
                    GetDaclSecInfo(daclHook, &daclPrev);

                uint pCurr = GetCurrentProcess();
                if ( GetDaclSecInfo(pCurr, &daclReplace) &&
                     SetDaclSecInfo(daclHook, daclReplace) ) // Replace the target process's security information with this process's security information
                {
                    CloseHandle(daclHook); // Close the process handle with DACL access
                    bool haveDaclAccess = false;
                    HookProcess(processID, minimumAccess, desiredAccess, ref haveDaclAccess);
                    if ( prevStored && haveDaclAccess && hookedProcess != NULL )
                        SetDaclSecInfo(hookedProcess, daclPrev);// Restore previous security
                }
                else
                    CloseHandle(daclHook); // Close the process handle with DACL access
            }
            return hookedProcess != NULL;
        }

        /// <summary>Attempts to get the DACL security information for the given processes</summary>
        /// <param name="hProcess">The process to get the DACL security information from</param>
        /// <param name="ppDacl">A pointer to the ACL pointer to be set to the security info</param>
        /// <returns>Whether the DACL security information was retrieved</returns>
        protected unsafe bool GetDaclSecInfo(uint hProcess, ACL** ppDacl)
        {
            uint secdesc; // Placeholder (required)
            uint secInfo = DACL_SECURITY_INFORMATION;
            uint result = GetSecurityInfo(hProcess, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, secInfo,
                NULL, NULL, (uint)(ppDacl), NULL, (uint)(&secdesc));
            LocalFree(secdesc); // Free the placeholder
            return result == ERROR_SUCCESS;
        }

        /// <summary>Attempts to set the DACL security information for the given processes</summary>
        /// <param name="hProcess">The process to set the DACL security information for</param>
        /// <param name="dacl">The new security information for a process</param>
        /// <returns>Whether the DACL security information was set successfully</returns>
        protected unsafe bool SetDaclSecInfo(uint hProcess, ACL* dacl)
        {
            uint secInfo = DACL_SECURITY_INFORMATION;
            uint result = SetSecurityInfo(hProcess, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, secInfo, NULL, NULL, dacl, NULL);
            return result == ERROR_SUCCESS;
        }

        /// <summary>Changes a privilege (used for debug privileges)</summary>
        /// <param name="hToken">The token to alter privileges for</param>
        /// <param name="Privilege">The privilege to set</param>
        /// <param name="bEnablePrivilege">The new state of the privilege</param>
        /// <returns>Whether the privilege was changed successfully</returns>
        protected static unsafe bool SetPrivilege(uint hToken, char* Privilege, uint bEnablePrivilege)
        {
            TokenPrivilege tp, tpPrevious;
            uint cbPrevious = (uint)sizeof(TokenPrivilege);
            LUID luid;
            if ( LookupPrivilegeValueW(NULL, (uint)(Privilege), (uint)(&luid)) != 0 )
            {
                tp.PrivilegeCount = 1;
                tp.Privileges.Luid = luid;
                tp.Privileges.Attributes = 0;

                AdjustTokenPrivileges(hToken, FALSE, (uint)(&tp), (uint)sizeof(TokenPrivilege),
                    (uint)(&tpPrevious), (uint)(&cbPrevious));

                if ( GetLastError() == ERROR_SUCCESS )
                {
                    tpPrevious.PrivilegeCount = 1;
                    tpPrevious.Privileges.Luid = luid;

                    if ( bEnablePrivilege == TRUE )
                        tpPrevious.Privileges.Attributes |= (SE_PRIVILEGE_ENABLED);
                    else
                        tpPrevious.Privileges.Attributes ^= (SE_PRIVILEGE_ENABLED & tpPrevious.Privileges.Attributes);

                    AdjustTokenPrivileges(hToken, FALSE, (uint)(&tpPrevious), cbPrevious, NULL, NULL);
                    return GetLastError() == ERROR_SUCCESS;
                }
            }
            return false;
        }

        /// <summary>Attempts to hook the process with the given accessLevel</summary>
        /// <param name="processID">The ID of the process to be hooked</param>
        /// <param name="accessLevel">The level of access with which to hook the process</param>
        /// <returns></returns>
        protected bool HookProcess(uint processID, uint accessLevel)
        {
            hookedProcess = OpenProcess(accessLevel, FALSE, processID);
            return hookedProcess != NULL;
        }

        /// <summary>
        /// Attempts to hook the process with at desiredAccess,
        /// or at least minimumAccess
        /// </summary>
        /// <param name="processID">The ID of the process to be hooked</param>
        /// <param name="minimumAccess">The minimum access to hook the process with</param>
        /// <param name="desiredAccess">The maximum access to hook the process with</param>
        /// <returns></returns>
        protected bool HookProcess(uint processID, uint minimumAccess, uint desiredAccess)
        {
            return HookProcess(processID, desiredAccess) ||
                HookProcess(processID, minimumAccess);
        }

        /// <summary>
        /// Attempts to hook the process with the given processID with the given access levels.
        /// The first attempt is with desiredAccess, then minimumAccess|WRITE_DAC, (if
        /// desiredAccess includes WRITE_DAC and minimumAccess does not) then minimumAccess
        /// </summary>
        /// <param name="processID">The ID of the process to be hooked</param>
        /// <param name="minimumAccess">The minimum access with which to hook the process</param>
        /// <param name="desiredAccess">The maximum access with which to hook the process</param>
        /// <param name="haveDaclAccess">Indicates whether WRITE_DAC privileges were obtained</param>
        /// <returns>Whether the process was hooked successfully</returns>
        protected bool HookProcess(uint processID, uint minimumAccess, uint desiredAccess, ref bool haveDaclAccess)
        {
            bool success = true;
            bool daclDesired = hasWriteDac(desiredAccess);
            bool daclMin = hasWriteDac(minimumAccess);
            haveDaclAccess = false;

            if ( HookProcess(processID, desiredAccess) )
                haveDaclAccess = daclDesired;
            else if ( daclDesired && !daclMin && HookProcess(processID, minimumAccess | WRITE_DAC) )
                haveDaclAccess = true;
            else if ( HookProcess(processID, minimumAccess) )
                haveDaclAccess = daclMin;
            else
                success = false;

            return success;
        }

        /// <summary>Returns whether the given accessLevel includes WRITE_DAC</summary>
        /// <param name="accessLevel">The access level to check for WRITE_DAC in</param>
        /// <returns>Whether accessLevel includes WRITE_DAC</returns>
        protected bool hasWriteDac(uint accessLevel)
        {
            return (accessLevel & WRITE_DAC) == WRITE_DAC;
        }

        /// <summary>Attempts to find the ID of a process with the given name</summary>
        /// <param name="szProcessName">The name of the process to search for</param>
        /// <param name="processID">Set to the ID of the process if found</param>
        /// <returns>Whether the process was found</returns>
        protected unsafe bool FindProcess(string szProcessName, ref uint processID)
        {
            uint hProcessSnap = NULL;
            ProcessEntry32 pe32 = new ProcessEntry32(); // Holds information about a process
            pe32.dwSize = (uint)sizeof(ProcessEntry32);

            hProcessSnap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0); // Get list of running processes
            if ( hProcessSnap != INVALID_HANDLE_VALUE ) // Got list successfully
            {
                if ( Process32FirstW(hProcessSnap, (uint)(&pe32)) == TRUE ) // If you can get information about the first process
                {
                    do // Step through processes
                    {
                        string str = new string(pe32.szExeFile);
                        if ( str.Equals(szProcessName) ) // If process name is the name you're searching for
                        {
                            processID = pe32.th32ProcessID; // Set returned process ID value
                            CloseHandle(hProcessSnap); // Cleanup process list
                            return true;
                        }
                    } while ( Process32NextW(hProcessSnap, (uint)(&pe32)) == TRUE ); // While there's more processes
                }

                CloseHandle(hProcessSnap); // Cleanup process list
            }
            return false;
        }

        /// <summary>
        /// Prints the details of the protection flags from a MEMORY_BASIC_INFORMATION struct
        /// </summary>
        /// <param name="protectionFlags"></param>
        protected void PrintProtectInfo(uint protectionFlags) // TODO
        {

        }

        /// <summary>Converts the given object to bytes</summary>
        /// <typeparam name="T">The type of the object to convert to bytes</typeparam>
        /// <param name="obj">The object to convert to bytes</param>
        /// <param name="bytes">The array that receives the object's bytes</param>
        /// <returns></returns>
        protected unsafe bool objectToBytes<T>(T obj, ref byte[] bytes)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            if ( typeof(T) == typeof(byte) )
                writer.Write((byte)(object)obj);
            else if ( typeof(T) == typeof(sbyte) )
                writer.Write((sbyte)(object)obj);
            else if ( typeof(T) == typeof(short) )
                writer.Write((short)(object)obj);
            else if ( typeof(T) == typeof(ushort) )
                writer.Write((ushort)(object)obj);
            else if ( typeof(T) == typeof(int) )
                writer.Write((int)(object)obj);
            else if ( typeof(T) == typeof(uint) )
                writer.Write((uint)(object)obj);
            else if ( typeof(T) == typeof(float) )
                return false; // floats?
            else if ( typeof(T) == typeof(double) )
                return false; // doubles?
            else if ( typeof(T) == typeof(decimal) )
                writer.Write((decimal)(object)obj);
            else if ( typeof(T) == typeof(char) )
                writer.Write((char)(object)obj);
            else if ( typeof(T) == typeof(string) )
                writer.Write((string)(object)obj);
            else if ( typeof(T) == typeof(bool) )
                writer.Write((bool)(object)obj);
            else if ( typeof(T) == typeof(object) )
                return false;
            else
            {
                GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    IntPtr bytePtr = handle.AddrOfPinnedObject();
                    Marshal.StructureToPtr(obj, bytePtr, false);
                }
                catch { return false; }
                finally { handle.Free(); }
                return true;
            }
            // structs?

            bytes = stream.ToArray();
            return true;
        }

        /// <summary>Converts an array of bytes to an object</summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="bytes">The bytes to be converted to an object</param>
        /// <param name="size">The size of the bytes to convert to an object</param>
        /// <param name="obj">Set to the object found in the array of bytes on success</param>
        /// <returns>Whether bytes were converted to an object successfully</returns>
        protected unsafe bool bytesToObject<T>(byte* bytes, uint size, ref T obj)
        {
            byte[] managedBytes;
            try { managedBytes = new byte[size]; }
            catch ( OutOfMemoryException ) { return false; }

            copyBuffer(bytes, managedBytes, size);

            MemoryStream stream = new MemoryStream(managedBytes);
            BinaryReader reader = new BinaryReader(stream);

            object result = null;
            if ( typeof(T) == typeof(byte) )
                result = reader.ReadByte();
            else if ( typeof(T) == typeof(sbyte) )
                result = reader.ReadSByte();
            else if ( typeof(T) == typeof(short) )
                result = reader.ReadInt16();
            else if ( typeof(T) == typeof(ushort) )
                result = reader.ReadUInt16();
            else if ( typeof(T) == typeof(int) )
                result = reader.ReadInt32();
            else if ( typeof(T) == typeof(uint) )
                result = reader.ReadUInt32();
            else if ( typeof(T) == typeof(float) )
                return false; // float?
            else if ( typeof(T) == typeof(double) )
                return false; // double?
            else if ( typeof(T) == typeof(decimal) )
                result = reader.ReadDecimal();
            else if ( typeof(T) == typeof(char) )
                result = reader.ReadChar();
            else if ( typeof(T) == typeof(string) )
                result = reader.ReadString();
            else if ( typeof(T) == typeof(bool) )
                result = reader.ReadBoolean();
            else
            {
                obj = (T)Marshal.PtrToStructure((IntPtr)bytes, typeof(T));
                return true;
            }

            obj = (T)result;
            return true;
        }

        /// <summary>Copies the 'from' buffer to the 'to' buffer</summary>
        /// <param name="from">The buffer to be copied from</param>
        /// <param name="to">The buffer to copy to</param>
        /// <param name="toSize">The size of the buffer to copy to</param>
        protected unsafe void copyBuffer(byte[] from, byte* to, uint toSize)
        {
            uint size = Math.Min((uint)from.Length, toSize);
            for ( uint i = 0; i < size; i++ )
                to[i] = from[i];
        }

        /// <summary>Copies the 'from' buffer to the 'to' buffer</summary>
        /// <param name="from">The buffer to be copied from</param>
        /// <param name="to">The buffer to copy to</param>
        /// <param name="fromSize">The size of the buffer to copy from</param>
        protected unsafe void copyBuffer(byte* from, byte[] to, uint fromSize)
        {
            uint size = Math.Min(fromSize, (uint)to.Length);
            for ( uint i = 0; i < size; i++ )
                to[i] = from[i];
        }

        /*  Windows details, imports, etc.
            Always refer to online documentation for full details.
            Pointers are often represented here as uint, watch
            for this when editing code in this class.
            
            Windows psedo equivilants (not comprehensive)...
            HWND = uint
            HANDLE = uint
            LONG = int
            int = int
            DWORD = uint
            const char* ~ string
        */

        public const uint PROCESS_ALL_ACCESS = 0x1FFFFF;
        public const uint PROCESS_ALL_LEGACY_ACCESS = 0x100FFB;
        public const uint WRITE_DAC = 0x40000;
        public const uint READ_CONTROL = 0x00020000;
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private static uint heap = GetProcessHeap();
        private const uint NULL = 0;
        private const uint TRUE = 1;
        private const uint FALSE = 0;
        private const uint TH32CS_SNAPPROCESS = 0x00000002;
        private const uint INVALID_HANDLE_VALUE = 0xFFFFFFFF;
        private const uint STILL_ACTIVE = 0x00000103;
        private const uint DACL_SECURITY_INFORMATION = 0x4;
        private const uint UNPROTECTED_DACL = 0x20000004;
        private const uint ERROR_SUCCESS = 0x0;
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint ERROR_NOT_SUPPORTED = 50;
        private const uint ERROR_NO_TOKEN = 1008;
        private const ushort SecurityImpersonation = 2;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const uint HEAP_ZERO_MEMORY = 0x00000008;
        private const string SE_DEBUG_NAME = "SeDebugPrivilege";

        public enum SE_OBJECT_TYPE
        {
            SE_KERNEL_OBJECT = 6
        };

        public enum SID_NAME_USE
        {

        };

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1
        };

        internal struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        internal struct LuidAndAttributes
        {
            public LUID Luid;
            public uint Attributes;
        }

        internal struct TokenPrivilege
        {
            public uint PrivilegeCount;
            public LuidAndAttributes Privileges; // Usually an array of privileges, only using one in this context
        }

        internal struct SID
        {

        }

        internal unsafe struct SidAndAttributes
        {
            public SID* Sid;
            public uint Attributes;
        }

        internal struct TokenUser
        {
            public SidAndAttributes User;
        }

        internal struct ACL
        {
            // As only the pointer is used, the details are unimportant
            /*public byte AclRevision;
            public byte Sbz1;
            public ushort AclSize;
            public ushort AceCount;
            public ushort Sbz2;*/
        }

        /// <summary>
        /// A structure mirroring the windows PROCESSENTRY32 struct
        /// </summary>
        internal unsafe struct ProcessEntry32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            public fixed char szExeFile[260];
        }

        internal unsafe struct FixedWideString
        {
            public fixed char str[260];
        }

        [DllImport("User32.dll")]
        public static extern int SetWindowLongA(uint hWnd, int nIndex, uint dwNew);
        [DllImport("User32.dll")]
        public static extern uint GetWindowThreadProcessId(uint hWnd, uint lpdwProcessId);
        [DllImport("User32.dll")]
        public static extern uint FindWindowW(uint lpClassName, uint lpWindowName);
        [DllImport("Kernel32.dll")]
        public static extern uint CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);
        [DllImport("Kernel32.dll")]
        public static extern uint CloseHandle(uint hObject);
        [DllImport("Kernel32.dll")]
        public static extern uint Process32FirstW(uint hSnapshot, uint lppe);
        [DllImport("Kernel32.dll")]
        public static extern uint Process32NextW(uint hSnapshot, uint lppe);
        [DllImport("Kernel32.dll")]
        public static extern uint GetProcessId(uint hProcess);
        [DllImport("Kernel32.dll")]
        public static extern uint GetExitCodeProcess(uint hProcess, uint lpExitCode);
        [DllImport("Kernel32.dll")]
        public static extern uint OpenProcess(uint dwDesiredAccess, uint bInheritHandle, uint dwProcessId);
        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentProcess();
        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentProcessId();
        [DllImport("Kernel32.dll")]
        public static extern uint LocalFree(uint hMem);
        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentThread();
        [DllImport("Kernel32.dll")]
        public static extern uint GetLastError();
        [DllImport("Kernel32.dll")]
        public static extern uint DebugActiveProcess(uint dwProcessId);
        [DllImport("Kernel32.dll")]
        public static extern uint DebugActiveProcessStop(uint dwProcessId);
        [DllImport("Kernel32.dll")]
        public static extern uint TerminateProcess(uint hProcess, uint uExitCode);
        [DllImport("Kernel32.dll")]
        public static extern uint HeapAlloc(uint hHeap, uint dwFlags, uint dwBytes);
        [DllImport("Kernel32.dll")]
        public static unsafe extern uint HeapFree(uint hHeap, uint dwFlags, byte* lpMem);
        [DllImport("Kernel32.dll")]
        public static unsafe extern uint ReadProcessMemory(uint hProcess, uint lpBaseAddress, byte* lpBuffer, uint nSize, uint lpNumberOfBytesRead);
        [DllImport("Kernel32.dll")]
        public static unsafe extern uint WriteProcessMemory(uint hProcess, uint lpBaseAddress, byte* lpBuffer, uint nSize, uint lpNumberOfBytesWritten);
        [DllImport("Kernel32.dll")]
        public static extern uint GetProcessHeap();
        [DllImport("Kernel32.dll")]
        public static unsafe extern uint QueryFullProcessImageNameW(uint hProcess, uint dwFlags, char* lpExeName, uint lpdwSize);
        [DllImport("Advapi32.dll")]
        public static extern uint GetSecurityInfo(uint handle, SE_OBJECT_TYPE ObjectType, uint SecurityInfo, uint ppsidOwner, uint ppsidGroup, uint ppDacl, uint ppSacl, uint ppSecurityDescriptor);
        [DllImport("Advapi32.dll")]
        public static unsafe extern uint SetSecurityInfo(uint handle, SE_OBJECT_TYPE ObjectType, uint SecurityInfo, uint psidOwner, uint psidGroup, ACL* pDacl, uint pSacl);
        [DllImport("Advapi32.dll")]
        public static extern uint OpenThreadToken(uint ThreadHandle, uint DesiredAccess, uint OpenAsSelf, uint TokenHandle);
        [DllImport("Advapi32.dll")]
        public static extern uint ImpersonateSelf(ushort ImpersonationLevel);
        [DllImport("Advapi32.dll")]
        public static extern uint LookupPrivilegeValueW(uint lpSystemName, uint lpName, uint lpLuid);
        [DllImport("Advapi32.dll")]
        public static extern uint AdjustTokenPrivileges(uint TokenHandle, uint DisableAllPrivileges, uint NewState, uint BufferLength, uint PreviousState, uint ReturnLength);
        [DllImport("Advapi32.dll")]
        public static extern uint IsValidAcl(uint pAcl);
        [DllImport("Advapi32.dll")]
        public static unsafe extern uint OpenProcessToken(uint ProcessHandle, uint DesiredAccess, uint* TokenHandle);
        [DllImport("Advapi32.dll")]
        public static unsafe extern uint GetTokenInformation(uint TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, void* TokenInformation, uint TokenInformationLength, uint* ReturnLength);
        [DllImport("Advapi32.dll")]
        public static unsafe extern uint LookupAccountSidW(char* lpSystemName, SID* lpSid, char* lpName, uint* cchName, char* lpReferencedDomainName, uint* cchReferencedDomainName, SID_NAME_USE* peUse);
    }

    /// <summary>
    /// A managed version of the windows PROCESSENTRY32 struct,
    /// uses a string rather than a character array for the exe
    /// file name, excludes unused fields and the struct size field,
    /// and simplifies names
    /// </summary>
    public struct ProcessEntry
    {
        /// <summary>
        /// th32ProcessID - The process identifier
        /// </summary>
        public uint ProcessId;

        /// <summary>
        /// cntThreads - The number of execution threads
        /// started by the process
        /// </summary>
        public uint NumThreads;

        /// <summary>
        /// th32ParentProcessID - The identifier of the process
        /// that created this process (its parent)
        /// </summary>
        public uint ParentProcessId;

        /// <summary>
        /// pcPriClassBase - The base priority of any threads
        /// created by this process
        /// </summary>
        public int BasePriority;

        /// <summary>
        /// szExeFile - The name of the executable file for
        /// the process; not necessarily complete,
        /// see online documentation for PROCESSENTRY32
        /// </summary>
        public string ExeFileName;
    }

    /// <summary>
    /// A class for finding the sizes of types and variables
    /// </summary>
    public static class Sizes
    {
        /// <summary>Retrieves the size of the generic type T</summary>
        /// <typeparam name="T">The type you are getting the size of</typeparam>
        /// <returns>The size of 'T' on success, 0 otherwise</returns>
        public static int SizeOf<T>()
        {
            return FetchSizeOf(typeof(T));
        }

        /// <summary>Retrieves the size of the type of obj</summary>
        /// <typeparam name="T">The type you will be getting the size of</typeparam>
        /// <param name="obj">The object whose type you will get the size ofwe</param>
        /// <returns>The size of 'obj' on success, 0 otherwise</returns>
        public static int SizeOf<T>(T obj)
        {
            return FetchSizeOf(typeof(T));
        }

        /// <summary>Retrieves the size of 'type'</summary>
        /// <param name="type">The type to get the size of</param>
        /// <returns>The size of the given type</returns>
        public static int SizeOf(this Type type)
        {
            return FetchSizeOf(type);
        }

        /// <summary>Gets the size of the specified type</summary>
        /// <param name="type">The type to get the size of</param>
        /// <returns>The size of the given type on success, 0 otherwise</returns>
        private static int FetchSizeOf(this Type type)
        {
            if ( typeSizeCache == null )
                CreateCache();

            if ( typeSizeCache != null )
            {
                int size = 0;
                if ( GetCachedSizeOf(type, out size) )
                    return size;
                else
                    return CalcAndCacheSizeOf(type);
            }
            else
                return CalcSizeOf(type);
        }

        /// <summary>Attempst to get the size of 'type' from the cache</summary>
        /// <param name="type">The type to get the size of</param>
        /// <param name="size">Set to the size of type on success, 0 otherwise</param>
        /// <returns>true on success, false otherwise</returns>
        private static bool GetCachedSizeOf(Type type, out int size)
        {
            size = 0;
            try
            {
                if ( type != null )
                {
                    if ( !typeSizeCache.TryGetValue(type, out size) )
                        size = 0;
                }
            }
            catch
            {
                /*  - Documented: ArgumentNullException
                    - No critical exceptions. */
                size = 0;
            }
            return size > 0;
        }

        /// <summary>
        /// Attemps to calculate the size of 'type', and caches
        /// the size if it is valid (size > 0)
        /// </summary>
        /// <param name="type">The type to get the size of</param>
        /// <returns>The calculated size on success, 0 otherwise</returns>
        private static int CalcAndCacheSizeOf(Type type)
        {
            int typeSize = 0;
            try
            {
                typeSize = CalcSizeOf(type);
                if ( typeSize > 0 )
                    typeSizeCache.Add(type, typeSize);
            }
            catch
            {
                /*  - Documented: ArgumentException, ArgumentNullException,
                    - Additionally Expected: OutOfMemoryException
                    - No critical exceptions documented. */
            }
            return typeSize;
        }

        /// <summary>Calculates the size of a type using dynamic methods</summary>
        /// <param name="type">The type to get the size of</param>
        /// <returns>The type's size on success, 0 otherwise</returns>
        private static int CalcSizeOf(this Type type)
        {
            try
            {
                var sizeOfMethod = new DynamicMethod("SizeOf", typeof(int), Type.EmptyTypes);
                var generator = sizeOfMethod.GetILGenerator();
                generator.Emit(OpCodes.Sizeof, type);
                generator.Emit(OpCodes.Ret);

                var sizeFunction = (Func<int>)sizeOfMethod.CreateDelegate(typeof(Func<int>));
                return sizeFunction();
            }
            catch
            {
                /*  - Documented: OutOfMemoryException, ArgumentNullException,
                                  ArgumentException, MissingMethodException,
                                  MethodAccessException
                    - No critical exceptions documented. */
            }
            return 0;
        }

        /// <summary>Attempts to allocate the typeSizesCache</summary>
        /// <returns>Whether the typeSizesCache is allocated</returns>
        private static void CreateCache()
        {
            if ( typeSizeCache == null )
            {
                try
                {
                    typeSizeCache = new Dictionary<Type, int>();
                }
                catch
                {
                    /*  - Documented: OutOfMemoryException
                        - No critical exceptions documented. */
                    typeSizeCache = null;
                }
            }
        }

        /// <summary>
        /// Static constructor for Sizes, sets typeSizeCache to null
        /// </summary>
        static Sizes()
        {
            CreateCache();
        }

        /// <summary>
        /// Caches the calculated size of various types
        /// </summary>
        private static Dictionary<Type, int> typeSizeCache;
    }
}
