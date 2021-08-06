using CESDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CE_EWND_plugin
{
    public partial class MainForm : Form
    {
        CESDKLua lua = CESDK.CESDK.currentPlugin.sdk.lua;
        IntPtr handle = new IntPtr();
        int processID = 0;
        int hwndInfo = -1;
        int loadFuc = -1;
        string enumWindows = "";
        public MainForm()
        {
            InitializeComponent();
        }

        async private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            bool temp = await Task.Run(() => GetWindows());
            bool temp2 = await Task.Run(() => EnumWindows());
            if (temp && temp2)
            {
                textBox1.Text = $"获取成功\r\n窗口信息:0x{Convert.ToString(hwndInfo, 16)}\r\n" +
                    $"载入函数:0x{Convert.ToString(loadFuc, 16)}\r\n{enumWindows}";
            }

        }
        bool EnumWindows()
        {
            enumWindows = "";
            comboBox1.Items.Clear();
            byte[] temp = new byte[4];
            int windowsI = 0;
            //MessageBox.Show(handle.ToString(),hwndInfo.ToString());
            if (!Kernel.ReadProcessMemory(handle, (IntPtr)(hwndInfo + 284), temp, 4, out _))
            {
                MessageBox.Show("枚举第一次读取失败" + Marshal.GetLastWin32Error().ToString());
                return false;
            }
            windowsI = BitConverter.ToInt32(temp, 0) >> 3;
            byte[] temp3 = new byte[4];
            if (!Kernel.ReadProcessMemory(handle, (IntPtr)(hwndInfo + 276), temp3, 4, out _))
            {
                MessageBox.Show("枚举第2次读取失败" + Marshal.GetLastWin32Error().ToString());
                return false;
            }
            byte[] temp4 = new byte[4];
            for (int i = 0; i < windowsI; i++)
            {
                Kernel.ReadProcessMemory(handle, (IntPtr)BitConverter.ToInt32(temp3, 0) + (i) * 4, temp4, 4, out _);
                enumWindows += $"{i + 1}. 窗口ID:0x{Convert.ToString(BitConverter.ToInt32(temp4, 0), 16)} \r\n";
                comboBox1.Items.Add(Convert.ToString(BitConverter.ToInt32(temp4, 0), 16));
            }
            enumWindows += $"枚举完毕，共发现{windowsI}个窗口ID\r\n";
            return true;
        }
         bool GetWindows()
        {
            Kernel.MEMORY_BASIC_INFORMATION mbi;
            handle = Kernel.OpenProcess(Kernel.ProcessAccessFlags.All, false, processID);
            if (Kernel.VirtualQueryEx(handle, (IntPtr)0x00401000, out mbi, (uint)Marshal.SizeOf(typeof(Kernel.MEMORY_BASIC_INFORMATION))) == 0)
            {

                MessageBox.Show("VirtualQueryEx错误" + Marshal.GetLastWin32Error().ToString());
                return false;
            }
            byte[] buf = new byte[(int)mbi.RegionSize];
            if (!Kernel.ReadProcessMemory(handle, (IntPtr)0x00401000, buf, (int)mbi.RegionSize, out _))
            {
                MessageBox.Show("第一次读取失败" + Marshal.GetLastWin32Error().ToString());
                return false;
            }
            int temp = FindIndex(buf, new byte[] { 139, 68, 36, 12, 139, 76, 36, 8, 139, 84, 36, 4, 80, 81, 82, 185 });
            if (temp == -1)
            {
                MessageBox.Show("首次没找到");
                return false;
            }
            byte[] temp2 = new byte[4];
            Kernel.ReadProcessMemory(handle, (IntPtr)0x00401000 + temp + 16, temp2, 4, out _);
            hwndInfo = BitConverter.ToInt32(temp2, 0);
            temp = FindIndex(buf, new byte[] { 131, 236, 12, 51, 192, 86, 139, 116, 36, 28, 87, 139, 124, 36, 24, 199,
                7, 0, 0, 0, 0, 139, 78, 20, 133, 201, 116, 19, 80, 139, 70, 12, 80, 104, 214, 7, 0, 0 });
            if (temp == -1)
            {
                hwndInfo = -1;
            }
            loadFuc = 0x00401000 + temp;
            return true;
        }
        static int FindIndex(byte[] array, byte[] array2)
        {
            int i, j;

            for (i = 0; i < array.Length; i++)
            {
                if (i + array2.Length <= array.Length)
                {
                    for (j = 0; j < array2.Length; j++)
                    {
                        if (array[i + j] != array2[j]) break;
                    }

                    if (j == array2.Length) return i;
                }
                else
                    break;
            }

            return -1;
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            lua.GetGlobal("getOpenedProcessID");
            if (lua.IsFunction(-1))
            {
                lua.PCall(0, 1);
                processID = (int)lua.ToInteger(-1);
            }
            else
                MessageBox.Show("获取进程ID失败");
            lua.Pop(1);

            lua.GetGlobal("getOpenedProcessHandle");
            if (lua.IsFunction(-1))
            {
                lua.PCall(0, 1);
                handle = (IntPtr)lua.ToInteger(-1);
            }
            else
                MessageBox.Show("Failure getting the ProcessHandle");

            lua.Pop(1);
            
        }
        int CallWindows(int windowsID, int loadFuc)
        {
            IntPtr tempAddr = Kernel.VirtualAllocEx(handle, (IntPtr)0, 1024, Kernel.AllocationType.Commit, Kernel.MemoryProtection.ExecuteReadWrite);
            if (tempAddr == IntPtr.Zero)
            {
                MessageBox.Show("VirtualAlloc失败");
                return -1;
            }
            List<byte> injectByteTemp = new List<byte>();
            injectByteTemp.AddRange(new byte[]{ 200, 0, 0, 0, 104, 2, 0, 0, 128, 104, 0, 0, 0, 0, 104, 1, 0, 0, 0,
                104, 0, 0, 0, 0, 104, 0, 0, 0, 0, 104, 0, 0, 0, 0, 104, 1, 0, 1, 0, 104, 2, 0, 1, 6, 104 });
            injectByteTemp.AddRange(BitConverter.GetBytes(windowsID));
            injectByteTemp.AddRange(new byte[] { 104, 3, 0, 0, 0, 187 });
            injectByteTemp.AddRange(BitConverter.GetBytes(loadFuc));
            injectByteTemp.AddRange(new byte[] { 232, 8, 0, 0, 0, 129, 196, 40, 0, 0, 0, 201, 195, 141, 68, 36, 8, 129, 236, 12, 0, 0, 0, 80, 255, 116,
                36, 20, 49, 192, 137, 68, 36, 8, 137, 68, 36, 12, 137, 68, 36, 16, 141, 84, 36, 8, 82, 255, 211, 139, 68, 36, 12, 139, 84, 36, 16, 139,
                76, 36, 20, 129, 196, 24, 0, 0, 0, 194, 4, 0 });
            Kernel.WriteProcessMemory(handle, tempAddr, injectByteTemp.ToArray(), injectByteTemp.Count, out _);
            IntPtr threadHandle = Kernel.CreateRemoteThread(handle, IntPtr.Zero, 0, tempAddr, IntPtr.Zero, 0, IntPtr.Zero);
            Kernel.WaitForSingleObject(threadHandle, 0xFFFFFFFF);
            Kernel.GetExitCodeThread(threadHandle, out uint exitCode);
            Kernel.CloseHandle(threadHandle);
            return 1;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            int code = await Task.Run(() => CallWindows(Convert.ToInt32((string)comboBox1.SelectedItem, 16), loadFuc));
            if(code ==1|| code == -1)
            {
                button2.Enabled = true;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("项目作者moshui\r\n吾爱破解ID：moshuiD\r\n部分代码来自吾爱破解ID：Pizza");
            Process.Start("https://github.com/moshuiD/CE-EWindow-Plugin");
        }
    }
}
