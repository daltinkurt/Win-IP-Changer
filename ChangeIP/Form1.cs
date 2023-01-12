using DnsHelperUI.WindowsFormsApplication_CS;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ChangeIP
{
    public partial class Form1 : Form
    {
        public string CurrentNic { get; set; } = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            NicleriGetir();
            DatasListele();
        }

        private void NicleriGetir()
        {
            lbNics.Items.Clear();
            var nics = NetworkManagement.GetAllNicDescriptions();
            lbNics.Items.AddRange(nics.Length > 0 ? nics : new string[] { "No Available NiCs" });
        }

        private void DatasListele()
        {
            lbDatas.Items.Clear();

            var datas = GetDatas();
            foreach (var d in datas)
            {
                lbDatas.Items.Add(d.Element("Baslik").Value);
            }
        }

        private void lbNics_Click(object sender, EventArgs e)
        {
            if (lbNics.SelectedItem != null)
                CurrentNic = lbNics.SelectedItem.ToString();
            else
                CurrentNic = null;
        }


        private List<XElement> GetDatas()
        {
            return (from x in XElement.Load(AppDomain.CurrentDomain.BaseDirectory + "DB.xml").Elements("Data")
                    orderby x.Element("Baslik").Value
                    select x).ToList();
        }

        private XDocument GetData()
        {
            return XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "DB.xml");
        }

        private void SaveData(XDocument root)
        {
            root.Save(AppDomain.CurrentDomain.BaseDirectory + "DB.xml");
        }

        private void btnEkle_Click(object sender, EventArgs e)
        {
            var baslik = txtBaslik.Text.Trim();
            var mevcut = GetDatas().FirstOrDefault(x => x.Element("Baslik").Value.Equals(baslik));
            if (mevcut != null)
            {
                MessageBox.Show("Bu başlık mevcut");
                return;
            }
            var ip = txtIP.Text.Trim();
            var maske = txtMaske.Text.Trim();
            var agGecidi = txtAgGecidi.Text.Trim();
            var dns1 = txtDNS1.Text.Trim();
            var dns2 = txtDNS2.Text.Trim();
            XElement el = new XElement("Data",
                new XElement("Baslik", baslik),
                new XElement("Ip", ip),
                new XElement("Maske", maske),
                new XElement("Gateway", agGecidi),
                new XElement("Dns1", dns1),
                new XElement("Dns2", dns2));
            var root = GetData();
            root.Root.Add(el);
            SaveData(root);
            MessageBox.Show("Kaydedildi");
            DatasListele();
            lbDatas.SelectedItem = lbDatas.Items.Cast<string>().FirstOrDefault(x => x.Equals(baslik));
        }

        private void btnGuncelle_Click(object sender, EventArgs e)
        {
            var baslik = txtBaslik.Text.Trim();
            var mevcut = GetDatas().FirstOrDefault(x => x.Element("Baslik").Value.Equals(baslik));
            if (mevcut == null)
            {
                MessageBox.Show("Bu başlık mevcut değil");
                return;
            }
            var ip = txtIP.Text.Trim();
            var maske = txtMaske.Text.Trim();
            var agGecidi = txtAgGecidi.Text.Trim();
            var dns1 = txtDNS1.Text.Trim();
            var dns2 = txtDNS2.Text.Trim();

            var root = GetData();
            var onceki = root.Root.Elements().FirstOrDefault(x => x.Element("Baslik").Value.Equals(baslik));
            onceki.Element("Ip").Value = ip;
            onceki.Element("Maske").Value = maske;
            onceki.Element("Gateway").Value = agGecidi;
            onceki.Element("Dns1").Value = dns1;
            onceki.Element("Dns2").Value = dns2;
            SaveData(root);
            MessageBox.Show("Kaydedildi");
            DatasListele();
            lbDatas.SelectedItem = lbDatas.Items.Cast<string>().FirstOrDefault(x => x.Equals(baslik));
        }

        private void btnSil_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Emin misiniz?", "Onay", MessageBoxButtons.OKCancel) != DialogResult.OK)
                return;
            var baslik = txtBaslik.Text.Trim();
            if (string.IsNullOrEmpty(baslik)) return;
            var root = GetData();
            var onceki = root.Root.Elements().FirstOrDefault(x => x.Element("Baslik").Value.Equals(baslik));
            if (onceki == null) return;
            onceki.Remove();
            SaveData(root);
            MessageBox.Show("Kaydedildi");
            DatasListele();

            txtAgGecidi.Text =
                txtBaslik.Text =
                txtDNS1.Text =
                txtDNS2.Text =
                txtIP.Text =
                txtMaske.Text = "";
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            DatasListele();
        }

        private void btnUygula_Click(object sender, EventArgs e)
        {
            if (lbNics.SelectedItem == null)
            {
                MessageBox.Show("Adaptör seçmelisiniz");
                return;
            }

            var nic = lbNics.SelectedItem.ToString();

            string ip = txtIP.Text?.Trim();
            if (string.IsNullOrWhiteSpace(ip) || !IsValidIPv4(ip))
            {
                MessageBox.Show(this, "IP adresi geçersiz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string maske = txtMaske.Text?.Trim();
            if (string.IsNullOrWhiteSpace(maske) || !IsValidIPv4(maske))
            {
                MessageBox.Show(this, "Alt ağ maskesi geçersiz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string agGecidi = txtAgGecidi.Text?.Trim();
            if (string.IsNullOrWhiteSpace(agGecidi) || !IsValidIPv4(agGecidi))
            {
                MessageBox.Show(this, "Varsayılan ağ geçidi geçersiz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var dnss = new List<string>();
            string dns1 = txtDNS1.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(dns1))
            {
                if (!IsValidIPv4(dns1))
                {
                    MessageBox.Show(this, "DNS 1 geçersiz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                dnss.Add(dns1);
            }

            string dns2 = txtDNS2.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(dns2))
            {
                if (!IsValidIPv4(dns2))
                {
                    MessageBox.Show(this, "DNS 2 geçersiz", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                dnss.Add(dns2);
            }

            try
            {
                btnUygula.Text = "Lütfen bekleyiniz";
                btnUygula.Enabled = false;
                NetworkManagement.SetIP(nic, new string[] { ip }, maske, agGecidi);
                NetworkManagement.SetNameservers(nic, dnss.ToArray(), restart: false);
                btnUygula.Text = "Uygula";
                btnUygula.Enabled = true;
                MessageBox.Show("Uygulandı");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: \n" + ex.Message);
            }
            finally
            {
                btnUygula.Text = "Uygula";
                btnUygula.Enabled = true;
            }
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            if (lbNics.SelectedItem == null)
            {
                MessageBox.Show("Adaptör seçmelisiniz");
                return;
            }
            var nic = lbNics.SelectedItem.ToString();
            NetworkManagement.RestartNetworkAdapter(nic);
        }

        private void pbNicYenile_Click(object sender, EventArgs e)
        {
            NicleriGetir();
        }

        private bool IsValidIPv4(string value)
        {
            // First check to see if there are at least three periods in the value
            if (value.Count(x => x == '.') != 3)
                return false;

            IPAddress address;
            return IPAddress.TryParse(value, out address) && address.AddressFamily == AddressFamily.InterNetwork;
        }

        private void btnCikis_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lbDatas_SelectedValueChanged(object sender, EventArgs e)
        {
            if (lbDatas.SelectedItem != null)
            {
                var s = lbDatas.SelectedItem.ToString();
                var el = GetDatas().FirstOrDefault(x => x.Element("Baslik").Value.Equals(s));
                txtBaslik.Text = el.Element("Baslik").Value;
                txtAgGecidi.Text = el.Element("Gateway").Value;
                txtDNS1.Text = el.Element("Dns1").Value;
                txtDNS2.Text = el.Element("Dns2").Value;
                txtIP.Text = el.Element("Ip").Value;
                txtMaske.Text = el.Element("Maske").Value;
            }
        }
    }
}