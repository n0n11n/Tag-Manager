using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
        
namespace TagTextBox

{
    //adapted source: https://stackoverflow.com/questions/1437002/winforms-c-sharp-autocomplete-in-the-middle-of-a-textbox
    // and https://github.com/teamalpha5441/SQLBooru/tree/master


    public class TagTextBox : TextBox
    {
        private internalTagListBox _listBox;
        private string _formerValue = string.Empty;
        private List<string> _Tags;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> Values
        {
            get { return _Tags; }
            set { _Tags = value == null ? new List<string> { } : value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control ListBoxParent
        {
            get 
            {
                if (_listBox.Parent == null)
                    _listBox.Parent = Parent;
                return _listBox.Parent;
            }
            set { _listBox.Parent = value ?? Parent; }
        }

        public TagTextBox()
        {
            _listBox = new internalTagListBox();
            _listBox.LegacyMode = true; //TODO Implement color
            _listBox.DoubleClick += new EventHandler(_listBox_DoubleClick);
            _listBox.LostFocus += new EventHandler(_LostFocus);
            LostFocus += new EventHandler(_LostFocus);
            KeyDown += this_KeyDown;
            KeyUp += this_KeyUp;
            ListBoxParent = null;
            ResetListBox();
        }

        private void _LostFocus(object sender, EventArgs e)
        {
            if (!(_listBox.Focused || this.Focused))
                ResetListBox();
        }

        void _listBox_DoubleClick(object sender, EventArgs e)
        {
            this_KeyDown(sender, new KeyEventArgs(Keys.Tab));
        }

        public void SetTags(List<string> Tags)
        {
            if (Tags == null)
                this.Values = null;
            else
            {
                List<string> tmp = new List<string>();
                foreach (string tag in Tags)
                    tmp.Add(tag);
                this.Values = tmp;
            }
        }

        private void ShowListBox()
        {
            Point p1 = PointToScreen(new Point(0, Height));
            Point p2 = ListBoxParent.PointToScreen(new Point(2, 2));
            _listBox.Location = Point.Subtract(p1, new Size(p2));
            _listBox.Visible = true;
            //_listBox.KeyDown += this_KeyDown;
            _listBox.BringToFront();
        }

        private void ResetListBox()
        {
            _listBox.Visible = false;
        }

        private void this_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateListBox();
        }

        private void this_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Tab:
                case Keys.Space:
                case Keys.Enter:
                    {
                        if (_listBox.Visible)
                        {
                            string selected = (string)_listBox.SelectedItem;
                            InsertWord((string)_listBox.SelectedItem);
                            ResetListBox();
                            _formerValue = Text;
                            e.SuppressKeyPress = true;  //get rid of error sound
                            if (e.KeyCode == Keys.Space)
                            {
                                e.SuppressKeyPress = false;
                            }
                            e.Handled = true;
                        }
                        break;
                    }
                case Keys.Down:
                    {
                        if ((_listBox.Visible) && (_listBox.SelectedIndex < _listBox.Items.Count - 1))
                            _listBox.SelectedIndex++;
                        break;
                    }
                case Keys.Up:
                    {
                        if ((_listBox.Visible) && (_listBox.SelectedIndex > 0))
                            _listBox.SelectedIndex--;
                        break;
                    }
            }

        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Tab:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        private void UpdateListBox()
        {
            if (Text == _formerValue) return;
            _formerValue = Text;
            string word = GetWord().ToLower();
            bool negate = word.StartsWith("-");
            if (Values != null && word.Length > 0)
            {
                List<string> matches = Values.FindAll(x => x.ToLower().Contains(negate ? word.Substring(1) : word));
                if (matches.Count > 0)
                {
                    ShowListBox();
                    _listBox.Items.Clear();
                    if (matches.Count > 15)
                        matches = matches.GetRange(0, 14);
                    //matches.Add("...");
                    matches.ForEach(x => _listBox.Items.Add(negate ? "-" + x : x));
                    _listBox.SelectedIndex = 0;
                    _listBox.Height = 0;
                    _listBox.Width = 0;
                    Focus();
                    using (Graphics graphics = _listBox.CreateGraphics())
                    {
                        for (int i = 0; i < _listBox.Items.Count; i++)
                        {
                            _listBox.Height += _listBox.GetItemHeight(i);
                            int itemWidth = (int)graphics.MeasureString(((string)_listBox.Items[i]) + "_", _listBox.Font).Width;
                            _listBox.Width = (_listBox.Width < itemWidth) ? itemWidth : _listBox.Width;
                        }
                    }
                }
                else
                    ResetListBox();
            }
            else
                ResetListBox();
        }

        private string GetWord()
        {
            string text = Text;
            int pos = SelectionStart;
            int posStart = text.LastIndexOf(' ', (pos < 1) ? 0 : pos - 1);
            posStart = (posStart == -1) ? 0 : posStart + 1;
            int posEnd = text.IndexOf(' ', pos);
            posEnd = (posEnd == -1) ? text.Length : posEnd;
            int length = ((posEnd - posStart) < 0) ? 0 : posEnd - posStart;
            return text.Substring(posStart, length).Trim();
        }

        private void InsertWord(string newTag)
        {
            string text = Text;
            int pos = SelectionStart;
            int posStart = text.LastIndexOf(' ', (pos < 1) ? 0 : pos - 1);
            posStart = (posStart == -1) ? 0 : posStart + 1;
            int posEnd = text.IndexOf(' ', pos);
            string firstPart = text.Substring(0, posStart) + newTag;
            string updatedText = firstPart + ((posEnd == -1) ? "" : text.Substring(posEnd, text.Length - posEnd));
            Text = updatedText;
            SelectionStart = firstPart.Length;
        }

        public List<string> SelectedValues
        {
            get
            {
                string[] result = Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return new List<string>(result);
            }
        }

        private class internalTagListBox : ListBox
        {
            public delegate Color GetItemColorHandler(int Index);
            public event GetItemColorHandler GetItemColor;

            public bool LegacyMode = false;

            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                if (LegacyMode)
                    base.OnDrawItem(e);
                else
                {
                    Graphics g = e.Graphics;
                    e.DrawBackground();
                    SolidBrush sb = new SolidBrush(e.ForeColor);
                    if (SelectedItem != Items[e.Index])
                    {
                        Color c = e.ForeColor;
                        if (GetItemColor != null)
                            c = GetItemColor(e.Index);
                        sb = new SolidBrush(c);
                    }
                    g.DrawString((string)Items[e.Index], e.Font, sb, e.Bounds.Location);
                    e.DrawFocusRectangle();
                }
            }
        }
    }
    
    //*/ Alternative implementation for Autocomplete Textbox
    /*
    class TagTextBox : TextBox
    {
        private ListBox _listBox;
        private bool _isAdded;
        private String[] _values;
        private String _formerValue = String.Empty;
        private int _prevBreak;
        private int _nextBreak;
        private int _wordLen;

        public TagTextBox()
        {
            InitializeComponent();
            ResetListBox();
        }

        private void InitializeComponent()
        {
            _listBox = new ListBox();
            KeyDown += this_KeyDown;
            KeyUp += this_KeyUp;
        }

        private void ShowListBox()
        {
            if (!_isAdded)
            {
                Form parentForm = FindForm();
                if (parentForm == null) return;

                parentForm.Controls.Add(_listBox);
                Point positionOnForm = parentForm.PointToClient(Parent.PointToScreen(Location));
                _listBox.Left = positionOnForm.X;
                _listBox.Top = positionOnForm.Y + Height;
                _isAdded = true;
            }
            _listBox.Visible = true;
            _listBox.BringToFront();
        }

        private void ResetListBox()
        {
            _listBox.Visible = false;
        }

        private void this_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateListBox();
        }

        private void this_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                case Keys.Tab:
                case Keys.Space:
                    {
                        if (_listBox.Visible)
                        {
                            Text = Text.Remove(_prevBreak == 0 ? 0 : _prevBreak + 1, _prevBreak == 0 ? _wordLen + 1 : _wordLen);
                            Text = Text.Insert(_prevBreak == 0 ? 0 : _prevBreak + 1, _listBox.SelectedItem.ToString());
                            ResetListBox();
                            _formerValue = Text;
                            Select(Text.Length, 0);
                            e.Handled = true;
                        }
                        break;
                    }
                case Keys.Down:
                    {
                        if ((_listBox.Visible) && (_listBox.SelectedIndex < _listBox.Items.Count - 1))
                            _listBox.SelectedIndex++;
                        e.Handled = true;
                        break;
                    }
                case Keys.Up:
                    {
                        if ((_listBox.Visible) && (_listBox.SelectedIndex > 0))
                            _listBox.SelectedIndex--;
                        e.Handled = true;
                        break;
                    }


            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Tab:
                    if (_listBox.Visible)
                        return true;
                    else
                        return false;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        private void UpdateListBox()
        {
            if (Text == _formerValue) return;
            if (Text.Length == 0)
            {
                _listBox.Visible = false;
                return;
            }

            _formerValue = Text;
            var separators = new[] { '|', '[', ']', '\r', '\n', ' ', '\t' };
            _prevBreak = Text.LastIndexOfAny(separators, CaretIndex > 0 ? CaretIndex - 1 : 0);
            if (_prevBreak < 1) _prevBreak = 0;
            _nextBreak = Text.IndexOfAny(separators, _prevBreak + 1);
            if (_nextBreak == -1) _nextBreak = CaretIndex;
            _wordLen = _nextBreak - _prevBreak - 1;
            if (_wordLen < 1) return;

            string word = Text.Substring(_prevBreak + 1, _wordLen);

            if (_values != null && word.Length > 0)
            {
                string[] matches = Array.FindAll(_values,
                    x => (x.ToLower().Contains(word.ToLower())));
                if (matches.Length > 0)
                {
                    ShowListBox();
                    _listBox.BeginUpdate();
                    _listBox.Items.Clear();
                    Array.ForEach(matches, x => _listBox.Items.Add(x));
                    _listBox.SelectedIndex = 0;
                    _listBox.Height = 0;
                    _listBox.Width = 0;
                    Focus();
                    using (Graphics graphics = _listBox.CreateGraphics())
                    {
                        for (int i = 0; i < _listBox.Items.Count; i++)
                        {
                            if (i < 20)
                                _listBox.Height += _listBox.GetItemHeight(i);
                            // it item width is larger than the current one
                            // set it to the new max item width
                            // GetItemRectangle does not work for me
                            // we add a little extra space by using '_'
                            int itemWidth = (int)graphics.MeasureString(((string)_listBox.Items[i]) + "_", _listBox.Font).Width;
                            _listBox.Width = (_listBox.Width < itemWidth) ? itemWidth : Width; ;
                        }
                    }
                    _listBox.EndUpdate();
                }
                else
                {
                    ResetListBox();
                }
            }
            else
            {
                ResetListBox();
            }
        }

        public int CaretIndex => SelectionStart;

        public String[] Values
        {
            get
            {
                return _values;
            }
            set
            {
                _values = value;
            }
        }
    }
    */
}