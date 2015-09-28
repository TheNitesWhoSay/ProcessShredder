using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using ProcessModder;
using Updating;

namespace ProcessShredder
{
    public partial class ProcessShredderGUI : Form, IUpdatable
    {
        /// <summary>
        /// An object that tells this GUI to update itself
        /// in the given intervals
        /// </summary>
        private Updater updater = null;
        
        /// <summary>Default constructor for Forms</summary>
        public ProcessShredderGUI()
        {
            InitializeComponent();
        }
        
        /// <summary>The load method called to initialize this GUI</summary>
        /// <param name="sender">unused</param>
        /// <param name="e">unused</param>
        private void ProcessShredderGUI_Load(object sender, EventArgs e)
        {
            SetDoubleBuffered(listProcesses);
            listProcesses.View = View.Details;
            listProcesses.GridLines = true;
            listProcesses.FullRowSelect = true;
            listProcesses.HideSelection = false;
            RefreshProcessList();

            updater = new Updater(this);
            updater.StartTimedUpdates(1000);
        }

      // Passive Methods
      
        /// <summary>Called by the updater in the assigned intervals</summary>
        public void TimedUpdate()
        {
            if ( !this.IsDisposed )
            {
                this.Invoke((MethodInvoker)delegate
                {
                    RefreshProcessList();
                });
            }
        }
        
        /// <summary>
        /// Updates the process list with details of processes
        /// currently running on the system
        /// </summary>
        public void RefreshProcessList()
        {
            int topIndex = getTopIndex();
            uint[] selectedProcessIds = getSelectedProcessIds();
            ProcessEntry[] processList = getSortedProcesses();
            ListViewItem[] listViewItems = getProcessListItems(processList);
            int[] selectionIndices = getSelectionIndices(selectedProcessIds, processList);

            listProcesses.BeginUpdate();
            listProcesses.Items.Clear();
            listProcesses.Items.AddRange(listViewItems);
            setTopIndex(listViewItems, topIndex);
            selectIndices(selectionIndices);
            listProcesses.EndUpdate();
        }

      // Helper Methods
      
        /// <summary>
        /// Gets a list of processes running in this windows instance
        /// sorted according to the current settings */
        /// </summary>
        /// <returns>A sorted list of processes running on the system</returns>
        private ProcessEntry[] getSortedProcesses()
        {
            List<ProcessEntry> processList = ProcessMod.getProcessList();
            var sortedProcessList = from entry in processList
                                    orderby entry.exeFileName ascending
                                    select entry;

            return sortedProcessList.ToArray<ProcessEntry>();
        }
        
        /// <summary>Gets the index of the top item visible in the process list</summary>
        /// <returns>
        /// The index of the top item visible in the process list if successful,
        /// 0 otherwise
        /// </returns>
        private int getTopIndex()
        {
            if ( listProcesses.TopItem != null )
                return listProcesses.TopItem.Index;
            else
                return 0;
        }
        
        /// <summary>Sets the top item visible in the process list by index</summary>
        /// <param name="items">A list of all the items in the process list</param>
        /// <param name="topIndex">The index of the top item in the process list</param>
        private void setTopIndex(ListViewItem[] items, int topIndex)
        {
            if ( topIndex >= 0 )
            {
                try
                {
                    if ( topIndex < items.Length )
                    {
                        // Bug fix: set to last item then to desired item
                        listProcesses.TopItem = items[items.Length - 1];
                        listProcesses.TopItem = items[topIndex];
                    }
                }
                catch { } // Ignore buggy exceptions
            }
        }
        
        /// <summary>Gets all the processIds of the processes selected in the list</summary>
        /// <returns>An array of all selected processIds on success, null otherwise</returns>
        private uint[] getSelectedProcessIds()
        {
            uint[] selectedIds = null;
            if ( listProcesses.SelectedIndices != null )
            {
                selectedIds = new uint[listProcesses.SelectedIndices.Count];
                int j = 0;
                foreach ( int index in listProcesses.SelectedIndices )
                {
                    selectedIds[j] = Convert.ToUInt32(listProcesses.Items[index].SubItems[1].Text);
                    j++;
                }
            }
            return selectedIds;
        }
        
        /// <summary>Gets all the processIds of the processes selected in the list</summary>
        /// <returns>A list of processIds</returns>
        private List<uint> getSelectedProcessIdsList()
        {
            return new List<uint>(getSelectedProcessIds());
        }
        
        /// <summary>
        /// Gets all ListView indices that correspond to
        /// valid processIds in 'processIds'
        /// </summary>
        /// <param name="processIds">The process ids to find list view indices for</param>
        /// <param name="processList">A list of processes running on the system</param>
        /// <returns>A list of ListView indices on success, null otherwise</returns>
        private int[] getSelectionIndices(uint[] processIds, ProcessEntry[] processList)
        {
            if ( processIds != null && processList != null )
            {
                List<int> selectionIndices = new List<int>();
                
                foreach ( uint processId in processIds )
                {
                    int selectionIndex = getProcessIndex(processId, processList);
                    if ( selectionIndex >= 0 )
                        selectionIndices.Add(selectionIndex);
                }

                return selectionIndices.ToArray();
            }
            return null;
        }
        
        /// <summary>
        /// Returns the index of the ProcessEntry in the processList that
        /// contains the given processId */
        /// </summary>
        /// <param name="processId">The process id to search for</param>
        /// <param name="processList">A list of processes to find processId in</param>
        /// <returns></returns>
        private int getProcessIndex(uint processId, ProcessEntry[] processList)
        {
            int foundAt = 0;
            foreach ( ProcessEntry entry in processList )
            {
                if ( processId == entry.processId )
                    return foundAt;
                else
                    foundAt++;
            }
            return -1;
        }
        
        /// <summary>Marks the given indices as selected in the process list</summary>
        /// <param name="indices">The indices to mark as selected</param>
        private void selectIndices(int[] indices)
        {
            if ( indices != null )
            {
                foreach ( int index in indices )
                    listProcesses.Items[index].Selected = true;
            }
        }
        
        /// <summary>Gets the items that make up the process list's contents</summary>
        /// <param name="processList">A list of processes to put in the ListView</param>
        /// <returns>The items that make up the process list's contents</returns>
        private ListViewItem[] getProcessListItems(ProcessEntry[] processList)
        {
            ListViewItem[] items = new ListViewItem[processList.Length];
            int i = 0;
            foreach ( ProcessEntry entry in processList )
            {
                string[] args = {
                        entry.exeFileName,
                        entry.processId.ToString(),
                        entry.parentProcessId.ToString(),
                        "Unknown"
                    };

                if ( i < items.Length )
                    items[i] = new ListViewItem(args);

                i++;
            }
            return items;
        }

        /// <summary>Finds all child processes for the given rootProcessIds</summary>
        /// <param name="searchList">The list of processes to search through for children</param>
        /// <param name="foundList">The returned list of child processes & the root processes</param>
        /// <param name="rootProcessIds">The list of ancestors for which you are finding descendents</param>
        /// <returns>Whether the operation was successful</returns>
        private bool findDescendants(List<ProcessEntry> searchList,
            out List<ProcessEntry> foundList, uint[] rootProcessIds)
        {
            foundList = new List<ProcessEntry>();
            List<uint> parentIds = new List<uint>(rootProcessIds);

            while ( parentIds.Count > 0 )
            {
                List<uint> newParentIds = new List<uint>();
                List<ProcessEntry> toTransfer = new List<ProcessEntry>();

                foreach ( uint parentId in parentIds )
                {
                    foreach ( ProcessEntry entry in searchList )
                    {
                        if ( parentId == entry.processId ) // entry has this parentId
                            toTransfer.Add(entry);
                        else if ( parentId == entry.parentProcessId ) // entry is a child of parentId
                        {
                            toTransfer.Add(entry);
                            newParentIds.Add(entry.processId);
                        }
                    }
                }
                
                foreach ( ProcessEntry entry in toTransfer )
                {
                    searchList.Remove(entry); // note: searchList is altered
                    foundList.Add(entry);
                }

                parentIds = newParentIds;
            }
            return true;
        }

        /// <summary>Finds all the processIds in the given process list</summary>
        /// <param name="processList">The process list to get the ids from</param>
        /// <returns>The ids of all the process in the given list</returns>
        private uint[] processIdsFromList(List<ProcessEntry> processList)
        {
            uint[] processIds = new uint[processList.Count];
            int i = 0;
            foreach ( ProcessEntry entry in processList )
            {
                processIds[i] = entry.processId;
                i++;
            }
            return processIds;
        }

        /// <summary>
        /// For every processId, the associated processes are hooked,
        /// frozen (by attaching a debugger), then killed
        /// </summary>
        /// <param name="processIdsToKill">A list of process ids to kill</param>
        /// <returns>Whether all the processes were killed</returns>
        private bool freezeAndKill(uint[] processIdsToKill)
        {
            bool success = false;
            ProcessMod[] processModders = null;
            try
            {
                processModders = new ProcessMod[processIdsToKill.Length];
                int i = 0;
                foreach ( uint processId in processIdsToKill )
                {
                    processModders[i] = new ProcessMod();
                    i++;
                }

                /* The following three steps (open, freeze, kill) should execute
                    in a way that minimizes the target processes reactability
                    some amount of optimization could be done here including:
                   - Get debug privileges only once before this begins
                   - Arrange the processes such that related groups are killed
                      together, parents first, smaller before larger groups
                   - Use less client-friendly open code (single access level)
                   - Load and use unamanged C code */
                i = 0;
                // Begin time-sensative area
                foreach ( ProcessMod pmod in processModders )
                {
                    pmod.openWithProcessID(processIdsToKill[i]);
                    i++;
                }
                foreach ( ProcessMod pmod in processModders )
                    pmod.freezeProcess();
                foreach ( ProcessMod pmod in processModders )
                    pmod.terminateProcess();
                // End time-sensative area

                success = true;
                foreach ( ProcessMod pmod in processModders )
                {
                    if ( pmod.isOpen() )
                        success = false;
                }
                foreach ( ProcessMod pmod in processModders )
                    pmod.close();
            }
            finally
            {
                foreach ( ProcessMod pmod in processModders )
                    pmod.Dispose();
            }
            return success;
        }

      // Callbacks
      
        /// <summary>Called in response to the End Processes button being clicked</summary>
        /// <param name="sender">unused</param>
        /// <param name="e">unused</param>
        private void buttonEndProcesses_Click(object sender, EventArgs e)
        {
            bool includeChilds = checkIncludeChilds.Checked;
            uint[] processIdsToKill = getSelectedProcessIds();
            if ( includeChilds )
            {
                List<ProcessEntry> processList = ProcessMod.getProcessList();
                List<ProcessEntry> processesToKill = null;
                if ( findDescendants(processList, out processesToKill, processIdsToKill) )
                    processIdsToKill = processIdsFromList(processesToKill);
            }

            freezeAndKill(processIdsToKill);
        }
        
      // Static, high potential for relocation
      
        /// <summary>
        /// Attempts to change the specified control to double buffered
        /// (reduces flicker) this change will not occur if in a terminal
        /// server session
        /// </summary>
        /// <param name="control">The control to be set to double buffered</param>
        public static void SetDoubleBuffered(Control control)
        {
            if ( SystemInformation.TerminalServerSession )
                return;

            System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance;

            try
            {
                System.Reflection.PropertyInfo prop =
                    typeof(Control).GetProperty("DoubleBuffered", flags);

                if ( prop != null )
                    prop.SetValue(control, true, null);
            }
            catch
            {
                /*  - Documented: AmbiguousMatchException, ArgumentNullException,
                                  ArgumentException, TargetException,
                                  TargetParameterCountException, MethodAccessException,
                                  TargetInvocationException
                    - Notes: For some applications, TargetException requires a general
                             catch, keep general to prevent unnecessary crashes
                    - No critical exceptions documented. */
            }
        }
    }
}
