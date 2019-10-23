using InputshareLib.Input;
using InputshareLib.Input.Hotkeys;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InputshareWindows.Server
{
    public partial class AssignHotkeyWindow : Form
    {
        public Hotkey AssignedKey { get; private set; } = new Hotkey(0, 0);
        private bool enteringHotkey = false;

        private HotkeyModifiers hotkeyMods = 0;
        private Keys pressedKey = 0;

        public AssignHotkeyWindow(string assignMessage)
        {
            InitializeComponent();
            this.TopMost = true;
            this.KeyPreview = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.Text = assignMessage;
            this.KeyDown += AssignHotkeyWindow_KeyDown;
        }

        private void AssignHotkeyWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!enteringHotkey)
                return;


            hotkeyMods = 0;
            if (e.Modifiers.HasFlag(Keys.Alt))
                hotkeyMods = hotkeyMods | HotkeyModifiers.Alt;
            if (e.Modifiers.HasFlag(Keys.Control))
                hotkeyMods = hotkeyMods | HotkeyModifiers.Ctrl;
            if (e.Modifiers.HasFlag(Keys.Shift))
                hotkeyMods = hotkeyMods | HotkeyModifiers.Shift;

            pressedKey = e.KeyCode;

            AssignedKey = new Hotkey((WindowsVirtualKey)pressedKey, hotkeyMods);
            button1.Text = AssignedKey.ToString();
            e.Handled = true;
        }

        private void AssignHotkeyWindow_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
           
            if (!enteringHotkey)
            {
                enteringHotkey = true;
                this.BackColor = Color.Gray;
            }
            else
            {
                enteringHotkey = false;
                this.Close();
            }
                
        }
    }
}
