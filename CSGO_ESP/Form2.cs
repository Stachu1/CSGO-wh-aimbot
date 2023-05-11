using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSGO_ESP
{
    public partial class Form2 : Form
    {

        public bool enable_ESP = true;
        public bool enable_HP = true;
        public bool enable_AimBot = true;
        public bool enable_Skin_changer = false;
        public int AimBot_key = 0x6;
        public int TriggerBot_key = 0x5;
        public int team_line_width = 1;
        public int enemy_line_width = 1;
        public int aimBotMaxAngle = 10;

        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            TopMost = true;
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                enable_ESP = true;
            }
            else
            {
                enable_ESP = false;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                enable_HP = true;
            }
            else
            {
                enable_HP = false;
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                enable_AimBot = true;
            }
            else
            {
                enable_AimBot = false;
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                enable_Skin_changer = true;
            }
            else
            {
                enable_Skin_changer = false;
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            team_line_width = trackBar1.Value;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            enemy_line_width = trackBar2.Value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms.OfType<Form1>().Single();
            int itemID = int.Parse(textBox3.Text);
            int skinID = int.Parse(textBox4.Text);
            float wear = 0.0000000001f;
            mainForm.SkinchangerButton(itemID, skinID, wear);
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            aimBotMaxAngle = trackBar3.Value;
        }
    }
}
