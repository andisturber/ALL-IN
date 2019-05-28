using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ALL_IN
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public partial class Main : Form
    {
        string yao_name = "全部";
        string lats = "";
        string lons = "";
        string oid = "";//前往GPSSPG自行获取
        string key = "";//前往GPSSPG自行获取

        List<Yao> yao = new List<Yao>();

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

        private const int VM_NCLBUTTONDOWN = 0XA1;//定义鼠标左键按下
        private const int HTCAPTION = 2;
        
        LocationService service;
        public Main()
        {
            InitializeComponent();
        }
        public class JsCallback
        {
            private Form ContainerForm { get; set; }


            public JsCallback(Form containerForm)
            {
                ContainerForm = containerForm;
            }
            public void minWin()
            {
                ContainerForm.WindowState = FormWindowState.Minimized;
            }
        }
        string json = "";
        public void Read(string path)
        {
            StreamReader sr = new StreamReader(path, Encoding.UTF8);
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                json += (line.ToString());
            }
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            this.webBrowser.ObjectForScripting = this;
            this.webBrowser.ScriptErrorsSuppressed = true;
            webBrowser.Navigate(@System.IO.Directory.GetCurrentDirectory() + "\\radar\\index.html");
            Read(@System.IO.Directory.GetCurrentDirectory() + "\\yao_id.json");
            labelTitle.Text = "ALL - IN";
            NativeLibraries.Load();
            Root rb = JsonConvert.DeserializeObject<Root>(json);
            for (int i = 0; i < rb.Data.Count; i++)
            {
                Yao tempyao = new Yao();
                tempyao.id = rb.Data[i].ID;
                tempyao.name = rb.Data[i].Name;
                yao.Add(tempyao);
                cbList.Items.Add(rb.Data[i].Name);
            }
            cbList.SelectedIndex = 0;
            service = LocationService.GetInstance();
            service.PrintMessageEvent = PrintMessage;
            service.ListeningDevice();
        }
        List<DataItem> lists = new List<DataItem>();
        public void list(string id, string endtime, string lat, string lng)
        {
            DataItem dataItem = new DataItem();
            dataItem.id = int.Parse(id);
            dataItem.name = id2name(dataItem.id);
            dataItem.endtime = endtime;
            dataItem.lat = lat;
            dataItem.lng = lng;
            lists.Add(dataItem);
        }
        public void position(string lat, string lng)
        {
            Location2 = getLocation("http://api.gpsspg.com/convert/coord/?oid=" + oid + "&key=" + key + "&from=3&to=0&latlng=" + lat + "," + lng);
            lons = (Location2.Longitude - double.Parse(lng)).ToString();
            lats = (Location2.Latitude - double.Parse(lat)).ToString();
        }
        public Location Location { get; set; } = new Location();
        public Location Location2 { get; set; } = new Location();

        public static Location getLocation(string url)
        {
            ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

            string URL = url;
            System.Net.WebClient myWebClient = new System.Net.WebClient();
            myWebClient.Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)");
            byte[] myDataBuffer = myWebClient.DownloadData(URL);
            string SourceCode = Encoding.GetEncoding("utf-8").GetString(myDataBuffer);
            Debug.WriteLine("返回值：" + SourceCode);
            Location Location = new Location();
            var rb = JObject.Parse(SourceCode);
            var result = JObject.Parse(rb["result"].ToString().Replace("[", "").Replace("]", ""));
            Location.Latitude = Convert.ToDouble(result["lat"].ToString());
            Location.Longitude = Convert.ToDouble(result["lng"].ToString());
            Debug.WriteLine("经度：" + result["lat"].ToString());

            return Location;
        }
        private string id2name(int id)
        {
            try
            {
                return yao.Find(x => x.id == id).name;
            }
            catch { return "未收录"; }
        }
        public void PrintMessage(string msg)
        {
            if (rtbxLog.InvokeRequired)
            {
                this.Invoke(new Action<string>(PrintMessage), msg);
            }
            else
            {
                rtbxLog.AppendText($"{DateTime.Now.ToString("HH:mm:ss")}：\r\n{msg}\r\n");
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            service.ClearLocation();
        }

        private void frmMain_Paint(object sender, PaintEventArgs e)
        {
            RoundFormPainter.Paint(sender, e);
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
        private void labelTitle_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage((IntPtr)this.Handle, VM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        private void btn_MouseEnter(object sender, EventArgs e)
        {
            ((PictureBox)sender).Image = (Image)Properties.Resources.ResourceManager.GetObject(((PictureBox)sender).Name + "_Move");
        }
        private void btn_MouseDown(object sender, MouseEventArgs e)
        {
            ((PictureBox)sender).Image = (Image)Properties.Resources.ResourceManager.GetObject(((PictureBox)sender).Name + "_Down");
        }
        private void btn_MouseLeave(object sender, EventArgs e)
        {
            ((PictureBox)sender).Image = (Image)Properties.Resources.ResourceManager.GetObject(((PictureBox)sender).Name + "_Up");
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        public static List<Location> locations = new List<Location>();

        private Random rd = new Random();

        private Color RandomColor()
        {
            return Color.FromArgb(rd.Next(0, 256), rd.Next(0, 256), rd.Next(0, 256));
        }
        private void cbList_SelectedIndexChanged(object sender, EventArgs e)
        {
            yao_name = cbList.SelectedItem.ToString();
        }

        private void dgvLocation_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            Location.Latitude = double.Parse(dgvLocation.SelectedRows[0].Cells[4].Value.ToString()) + double.Parse(lats);
            Location.Longitude = double.Parse(dgvLocation.SelectedRows[0].Cells[3].Value.ToString()) + double.Parse(lons);
            service.UpdateLocation(Location);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Search();
            if (cbList.SelectedIndex == 0)
            {
                for (int i = 0; i < this.dgvLocation.Rows.Count; ++i)
                {
                    dgvLocation.Rows[i].Visible = true;
                }
            }
            else
            {
                for (int i = 0; i < this.dgvLocation.Rows.Count; ++i)
                {
                    if ((string)dgvLocation.Rows[i].Cells["name"].Value == yao_name)
                    {
                        dgvLocation.Rows[i].Visible = true;
                    }
                    else
                    {
                        CurrencyManager cm = (CurrencyManager)BindingContext[dgvLocation.DataSource];

                        cm.SuspendBinding(); //挂起数据绑定

                        dgvLocation.Rows[i].Visible = false;

                        cm.ResumeBinding(); //恢复数据绑定

                    }
                }
            }
            label1.Text = "共有" + dgvLocation.RowCount + "条记录。";
        }
        private DataTable ToDataTable<T>(List<T> items)

        {

            var tb = new DataTable(typeof(T).Name);



            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);



            foreach (PropertyInfo prop in props)

            {

                Type t = GetCoreType(prop.PropertyType);

                tb.Columns.Add(prop.Name, t);

            }



            foreach (T item in items)

            {

                var values = new object[props.Length];



                for (int i = 0; i < props.Length; i++)

                {

                    values[i] = props[i].GetValue(item, null);

                }



                tb.Rows.Add(values);

            }



            return tb;

        }
        public static bool IsNullable(Type t)

        {

            return !t.IsValueType || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));

        }
        public static Type GetCoreType(Type t)

        {

            if (t != null && IsNullable(t))

            {

                if (!t.IsValueType)

                {

                    return t;

                }

                else

                {

                    return Nullable.GetUnderlyingType(t);

                }

            }

            else

            {

                return t;

            }

        }
        private void Search()
        {
                lists.Distinct().ToList();
                dgvLocation.DataSource = ToDataTable(lists);
        }
    }
    public class DataItems
    {
        /// <summary>
        /// 
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// 三魂干将
        /// </summary>
        public string Name { get; set; }
    }

    public class Root
    {
        /// <summary>
        /// 
        /// </summary>
        public List<DataItems> Data { get; set; }
    }
    public class DataItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string endtime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string lng { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string lat { get; set; }
    }
    public class Yao
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
    }
}
