using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace E705Proxy
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            notifyInit();
            proxys = new IProxy[]{
                new Proxy2(13000),
                new Proxy2(515)
            };
        }




        /// <summary>
        /// 初始化托盘图标功能
        /// </summary>
        private void notifyInit()
        {
            MenuItem miShowHide = new MenuItem("显示/隐藏窗体");
            miShowHide.Click += (obj, ea) =>
            {
                if (Visible)
                    Hide();
                else
                {
                    Show();
                    Activate();
                }
            };
            MenuItem miExit = new MenuItem("退出");
            miExit.Click += (obj, ea) => this.Close();

            VisibleChanged += (o, e) =>
            {
                ShowInTaskbar = Visible;
                miShowHide.Text = Visible ? "隐藏窗体" : "显示窗体";
            };

            notifyIcon1.ContextMenu = new System.Windows.Forms.ContextMenu(new MenuItem[] { miShowHide, miExit });
            notifyIcon1.DoubleClick += (o, e) => { Show(); Activate(); };

            notifyIcon1.Visible = true;
            Hide();
        }
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;
            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_CLOSE && notifyIcon1.Visible)
            {
                // 拦截关闭窗口消息，改为隐藏窗口
                this.Hide();
                return;
            }
            base.WndProc(ref m);
        }





        private IProxy[] proxys;
        private long[] lastBytesTrans;
        private void timer1_Tick(object sender, EventArgs e)
        {
            Func<long, string> byte2String = n =>
            {
                if (n < 1)
                    return "0B";
                string[] suffix = new string[] { "B", "kB", "MB", "GB", "TB", "PB", "EB" };
                int mag = (int)(Math.Log(n) / Math.Log(1024));
                if (mag >= suffix.Length)
                    mag = suffix.Length - 1;
                return string.Format("{0:G4}{1}", n / Math.Pow(1024, mag), suffix[mag]);
            };
            if (proxys == null)
                return;
            if (lastBytesTrans == null)
                lastBytesTrans = new long[proxys.Length];

            StringBuilder sb = new StringBuilder("端口号 连接数   即时速度 总数据量");
            for(int i = 0; i < proxys.Length; i++)
            {
                int connections;
                if (proxys[i] is Proxy)
                    connections = (proxys[i] as Proxy).Connections;
                else
                    connections = (proxys[i] as Proxy2).Connections.Count;
                sb.AppendFormat("\n{0,6} {1,6} {2,8}/s {3,8}",
                    proxys[i].Port,
                    connections,
                    byte2String((long)((proxys[i].BytesTrans - lastBytesTrans[i]) * 1000.0 / timer1.Interval)),
                    byte2String(proxys[i].BytesTrans));
                lastBytesTrans[i] = proxys[i].BytesTrans;
            }
            label1.Text = sb.ToString();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (proxys != null)
                foreach (var p in proxys)
                    p.Stop();
        }

    }
}
