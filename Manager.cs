using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TagManager
{
    public partial class Manager : Form
    {
        public Manager()
        {
            InitializeComponent();

            var ofd = new FolderSelect.FolderSelectDialog(); //Wraps System.Windows.Forms.OpenFileDialog to make it present a vista-style dialog.


            //IDEA: Text box linked to hidden autocomplete textbox for multiword autocomplete

            textBox1.Focus(); //TagTexBox has multiword autocomplete
            textBox1.Text = "";
            


            if (ofd.ShowDialog()) //Check that user chose a directory
            {
                Tags.Load(ofd.FileName);
                textBox1.Values = Tags.Get("<Tags>"); //load autofill values for TagTextBox
                //this.listBox1.Items.AddRange((ListBox.ObjectCollection)Tags.Get("<Tags>"));
                listBox1.DataSource = Tags.Get("<Tags>"); //load tags into sidelist
                CreateHeadersAndFillListView(listView1);  //setup listview
                PaintListView(ofd.FileName, listView1);  //populate listview with folder-item-info and tags
            }
            else
            {
                MessageBox.Show("You need to choose a Directory");
                System.Windows.Forms.Application.Exit();
                System.Environment.Exit(1);
            }



        }

        //adapted source: http://www.java2s.com/Code/CSharp/GUI-Windows-Form/UseListViewItemtodisplayfileinformation.htm
        private void CreateHeadersAndFillListView(ListView listView)
        {
            ColumnHeader colHead;

            colHead = new ColumnHeader();
            colHead.Text = "Filename";
            colHead.Tag = "";
            listView.Columns.Add(colHead);

            colHead = new ColumnHeader();
            colHead.Text = "Size";
            colHead.Tag = "";
            listView.Columns.Add(colHead);

            colHead = new ColumnHeader();
            colHead.Text = "Last accessed";
            colHead.Tag = "";
            listView.Columns.Add(colHead);

            colHead = new ColumnHeader();
            colHead.Text = "Tags";
            colHead.Tag = "";
            listView.Columns.Add(colHead);

            ColumnClickHandler.ColumnClickHandlerAttach(listView);

        }

        /// <summary>
        /// Populate listview with directory-item-info and tags and setup sorting 
        /// </summary>
        /// <param name="root">the path to the directory chosen by the user</param>
        /// <param name="lv">the listview to populate with the info</param>
        private void PaintListView(string root, ListView lv)
        {
            try
            {
                ListViewItem lvi;
                ListViewItem.ListViewSubItem lvsi;



                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(root);

                DirectoryInfo[] dirs = dir.GetDirectories();
                FileInfo[] files = dir.GetFiles();  //Get Files of directory

                lv.Items.Clear();

                lv.BeginUpdate();

                foreach (System.IO.FileInfo fi in files) //populate list
                {
                    lvi = new ListViewItem();
                    lvi.Text = fi.Name;
                    lvi.ImageIndex = 1;
                    lvi.Tag = fi.Name;

                    lvsi = new ListViewItem.ListViewSubItem();
                    lvsi.Text = Utilities.SizeSuffix(fi.Length); //e.g. "10 MB"
                    lvsi.Tag = fi.Length; //size in bytes as (long)
                    lvi.SubItems.Add(lvsi);

                    lvsi = new ListViewItem.ListViewSubItem();
                    lvsi.Text = fi.LastAccessTime.ToString(); // e.g. "18.11.1999 18:10"
                    lvsi.Tag = fi.LastAccessTime.Ticks; //ticks since 1970 till last acess time
                    lvi.SubItems.Add(lvsi);

                    lvsi = new ListViewItem.ListViewSubItem();
                    lvsi.Text = Tags.GetString(fi.Name);  // e.g. "System, TODO"
                    lvsi.Tag = new long(); //TODO: useful sorting Tag. For now just sort by tag string
                    lvi.SubItems.Add(lvsi);

                    lv.Items.Add(lvi);
                }
                lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent); //resize to content width
                lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize); //resize to header width
                lv.EndUpdate();
                var tmpLVI = new ListViewItem[lv.Items.Count];
                lv.Items.CopyTo(tmpLVI, 0);
                listviewBKP = tmpLVI.ToList(); //Backup of ListView for filtering function
            }
            catch (System.Exception err)
            {
                MessageBox.Show("Error: " + err.Message);
            }

            lv.View = View.Details;
        }


        // ColumnClick event handler for sorting
        static class ColumnClickHandler
        {
            private static ListView lv;
            public static void ColumnClickHandlerAttach(ListView lv)
            {
                lv.ColumnClick += new ColumnClickEventHandler(ColumnClick);
                ColumnClickHandler.lv = lv;

            }
            static bool reRun = true;
            static int lastCol = 0;
            static void ColumnClick(object o, ColumnClickEventArgs e)
            {
                if (reRun && lastCol == e.Column) //Sort descendig when same column is clicked and the column is already sorted ascending
                {
                    lv.ListViewItemSorter = new ListViewItemComparer(e.Column,true);
                    reRun = false;

                }
                else
                {
                    // Set the ListViewItemSorter property to a new ListViewItemComparer 
                    // object. Setting this property immediately sorts the 
                    // ListView using the ListViewItemComparer object.
                    lv.ListViewItemSorter = new ListViewItemComparer(e.Column,false);
                    reRun = true;
                    lastCol = e.Column;
                }

            }
        }


        /// <summary>
        /// Uses LVI.tag to compare the size and date as numbers and name and tags as text and reverses sort order on reRun
        /// </summary>
        class ListViewItemComparer : IComparer
        {
            private int col;
            private bool reRun = false;
            public ListViewItemComparer()
            {
                col = 0;
            }
            public ListViewItemComparer(int column,bool rr)
            {
                col = column;
                this.reRun = rr;
            }
            public int Compare(object x, object y) 
            {
                if (col == 0 || col == 3) 
                {
                    if (reRun) return String.Compare(((ListViewItem)y).SubItems[col].Text, ((ListViewItem)x).SubItems[col].Text); //Tag gave an error so compare .Text

                    return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
                }
                else
                {
                    if (reRun) return ((long)((ListViewItem)y).SubItems[col].Tag).CompareTo((long)((ListViewItem)x).SubItems[col].Tag);

                    return ((long)((ListViewItem)x).SubItems[col].Tag).CompareTo((long)((ListViewItem)y).SubItems[col].Tag); //compare using Long.CompareTo()
                }
            }
        }

        /// <summary>
        /// Class to hold TagDB and provide acessfuntions
        /// </summary>
        static class Tags
        {
            private static Dictionary<string, List<string>> TagDB; // = Utilities.loadTags();
            private static string _DBpath; //path to the folder of the TagDB.json file

            /// <summary>
            /// Loads the tags from the json file
            /// </summary>
            /// <param name="DBpath">path to the folder of the TagDB.json file</param>
            public static void Load(string DBpath)
            {
                TagDB = Utilities.loadTags(DBpath);
                _DBpath = DBpath;
            }
            /// <summary>
            /// Saves the tags to the json file that was previously loaded
            /// </summary>
            public static void Save()
            {
                Utilities.saveTags(TagDB,_DBpath);
            }

            /// <summary>
            /// Returns the tags that belong to a file
            /// </summary>
            /// <param name="Key">the name of the file is the key for the DB</param>
            /// <returns></returns>
            public static List<string> Get(string Key)
            {
                if (!TagDB.ContainsKey(Key))
                {
                    TagDB.Add(Key, new List<string>());
                }
                return TagDB[Key];
            }
            /// <summary>
            /// Handle adding of tags to the DB
            /// </summary>
            /// <param name="Key">filename</param>
            /// <param name="newTags">tags to be added</param>
            public static void Add(string Key,List<string> newTags)
            {
                if (!TagDB.ContainsKey(Key))
                {
                    TagDB.Add(Key, newTags);
                }
                else
                {
                    TagDB[Key] = newTags;
                }

                TagDB["<Tags>"] = TagDB["<Tags>"].Union(newTags).ToList<string>(); //remove duplicates


            }
            /// <summary>
            /// Return tags as string delimited by ", "
            /// </summary>
            /// <param name="Key">filename</param>
            /// <returns></returns>
            public static string GetString(string Key)
            {
                if (!TagDB.ContainsKey(Key)) return "";

                return String.Join(", ",TagDB[Key]);
            }

        }

        /// <summary>
        /// Add Tags button handler: adds textbox-tags to tagDB
        /// </summary>
        /// <param name="sender">the button</param>
        /// <param name="e">click event</param>
        private void addTag_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() == "") return; //on empty textbox just return

            List<string> newTags = textBox1.Text.Trim().Split(' ').ToList();
            foreach ( ListViewItem lvi in listView1.Items)
            {
                if(lvi.Checked || lvi.Selected)
                {
                    string Key = lvi.SubItems[0].Text; //Key is filename
                    
                    newTags = newTags.Union(Tags.Get(Key)).ToList<string>();//removes duplicates
                    newTags.Sort();
                    Tags.Add(Key, newTags);
                    lvi.SubItems[3].Text = Tags.GetString(Key); //return updated tags to listview
                    lvi.Checked = false;
                    
                }
            }
            textBox1.Text = "";
            textBox1.Values = Tags.Get("<Tags>"); //update the autosuggest
            listBox1.DataSource = Tags.Get("<Tags>"); //update TagListBox

            Tags.Save(); //TODO only save on change
        }


        /// <summary>
        /// Add tags from sidelist to textbox
        /// </summary>
        /// <param name="sender">listbox</param>
        /// <param name="e">click event</param>
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            
            //MessageBox.Show(lb.SelectedItem.ToString());

            textBox1.Text = (textBox1.Text.Trim() +" "+ lb.SelectedItem.ToString()).Trim(); //TODO: handle duplicates (currently handled everywhere else)
        }

        public List<ListViewItem> listviewBKP; //needed for the filter function

        /// <summary>
        /// Filters the list view by the tags in the textbox
        /// </summary>
        /// <param name="sender">filter button</param>
        /// <param name="e">click event</param>
        private void filterButton_Click(object sender, EventArgs e) //TODO: OR-filter
        {
            var listViewItemArray = new ListViewItem[listView1.Items.Count];
            listView1.Items.CopyTo(listViewItemArray,0);
            listView1.Items.Clear();
            foreach(ListViewItem lvi in listViewItemArray)
            {
                listviewBKP.Remove(lvi); //important when filtering an already filtered list
            }
            var listViewItemList = listViewItemArray.ToList();
            listViewItemList.AddRange(listviewBKP); //join shown with unshown list view items
            listviewBKP = listViewItemList;

            
            var filterTagList = textBox1.Text.Trim().Split(' ').ToHashSet().ToList(); //hashset removes duplicates from textbox-tags
            filterTagList.Sort();
            string filter = string.Join(", ", filterTagList);
            var filteredList = listViewItemList.FindAll(x => x.SubItems[3].Text == filter).ToArray(); //e.g.: filter by "System, TODO"
            listView1.Items.AddRange(filteredList);
        }

        /// <summary>
        /// Adds the unshown files back to the listview from listviewBKP
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">click event</param>
        private void resetButton_Click(object sender, EventArgs e)
        {
            var listViewItemArray = new ListViewItem[listView1.Items.Count];
            listView1.Items.CopyTo(listViewItemArray, 0);
            listView1.Items.Clear();
            foreach (ListViewItem lvi in listViewItemArray)
            {
                listviewBKP.Remove(lvi); //important when filtering an already filtered list
            }
            var listViewItemList = listViewItemArray.ToList();
            listViewItemList.AddRange(listviewBKP); //join shown with unshown list view items
            listviewBKP = listViewItemList;
            listView1.Items.AddRange(listViewItemList.ToArray());
        }
        /// <summary>
        /// Removes tags from file and add the tags to the textbox
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">click event</param>
        private void removeButton_Click(object sender, EventArgs e)
        {
            

            List<string> oldTags = textBox1.Text.Trim().Split(' ').ToList();
            foreach (ListViewItem lvi in listView1.Items)
            {
                if (lvi.Checked || lvi.Selected)
                {
                    string[] itemTags = lvi.SubItems[3].Text.Split(new string[]{ ", "},StringSplitOptions.RemoveEmptyEntries); //get old tags

                    oldTags = oldTags.Union(itemTags).ToList<string>();//removes duplicates
                    Tags.Add(lvi.SubItems[0].Text, new List<string>());//remove tags from tagDB
                    lvi.SubItems[3].Text = ""; //remove tags from view
                    lvi.Checked = false;

                }
            }
            oldTags.Sort();
            textBox1.Text = String.Join(" ", oldTags);


            Tags.Save(); //TODO only save on change
        }


        /// <summary>
        /// Loads tags from the listview if item is doubleclicked
        /// </summary>
        /// <param name="sender">listview</param>
        /// <param name="e">click event</param>
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var lvi = (sender as ListView).SelectedItems[0]; //get the doubleclicked item by checking for selection
            string[] itemTags = lvi.SubItems[3].Text.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            textBox1.Text = String.Join(" ", itemTags);
            lvi.Checked = !lvi.Checked;
        }

        

    }
}
