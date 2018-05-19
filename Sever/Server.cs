using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Security.Cryptography;

namespace Sever
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Connect();
        }
        string strkey = "000000000000000000000000000000";
        IPEndPoint ipe;
        Socket server;
        List<Socket> clientlist;
        void Connect()
        {
            clientlist = new List<Socket>();
            ipe = new IPEndPoint(IPAddress.Any, 9999);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            server.Bind(ipe);
            Thread Listen = new Thread(() => {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        clientlist.Add(client);
                        
                        
                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    ipe = new IPEndPoint(IPAddress.Any, 9999);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }

            });
            Listen.IsBackground = true;
            Listen.Start();

        }
        void Close()// dong ket noi
        {
            server.Close();
        }

        void Send(Socket client, string text, int TLNopadding)
        {
            if (client != null && txtMessage.Text != string.Empty)
                client.Send(Encoding.ASCII.GetBytes(text + ";" + TLNopadding + "," + Hash(txtMessage.Text)));
        }
        void SendNoise(Socket client, string text, int TLNopadding)
        {
            if (client != null && txtMessage.Text != string.Empty)
                client.Send(Encoding.ASCII.GetBytes(strBuilder.ToString() + ";" + TLNopadding + "," + Hash(txtMessage.Text)));
        }
        void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5];
                    client.Receive(data);
                    string message = Encoding.ASCII.GetString(data); // ep kieu object thanh string vi gui nhan tin nhan kieu string
                    
                    foreach (Socket item in clientlist)
                    {
                        if (item != null && item != client)
                            item.Send(Encoding.ASCII.GetBytes(txtMessage.Text));
                    }
                    AddMessage(message);
                }
            }
            catch
            {
                clientlist.Remove(client);
                client.Close();
            }
        }
        void AddMessage(string s)
        {
            lsvMessage.Items.Add(new ListViewItem() { Text = s });
        } //add message vao khung chat
        byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;
                    AES.Padding = PaddingMode.None;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }
        byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }
        
        public string EncryptText(string input, string key)
        {
            // Get the bytes of the string
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
            byte[] KeyBytes = Encoding.UTF8.GetBytes(key);
            

            // Hash the password with SHA256
            KeyBytes = SHA256.Create().ComputeHash(KeyBytes);

            byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, KeyBytes);

            string result = Convert.ToBase64String(bytesEncrypted);

            return result;
        }
        public string DecryptText(string input, string password)
        {
            // Get the bytes of the string
            byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            string result = Encoding.UTF8.GetString(bytesDecrypted);

            return result;
        }

        private void Sever_FormClosed(object sender, FormClosedEventArgs e)
        {
            Close();
        }


        private void btnSend_Click(object sender, EventArgs e)// gui tin cho tat ca client
        {
            if (txtMessage.Text != string.Empty)
            {
                string tmp = PaddingText(txtMessage.Text);
                foreach (Socket item in clientlist)
                {
                    Send(item, tmp, txtMessage.Text.Length);
                }
                AddMessage(txtMessage.Text);
                txtMessage.Clear();
            }

        }


        //time stmp
        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        //Padiing
        public string PaddingText(string text)
        {
            int blockmissing = 16 - (16 % text.Length);
            string Ptext;
            if (blockmissing != 0)
            {
                String timeStamp = GetTimestamp(DateTime.Now);
                Ptext = timeStamp.Substring(0, blockmissing);
                return String.Concat(text, Ptext);
            }
            else return text;
        }
        // Send Noise
        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        public string Hash(string text)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                return GetMd5Hash(md5Hash, text);
            }
        }
        public static char GetChar()
        {
            string chars = "$%#@!*abcdefghijklmnopqrstuvwxyz1234567890?;:ABCDEFGHIJKLMNOPQRSTUVWXYZ^&";
            Random rand = new Random();
            int num = rand.Next(0, chars.Length - 1);
            return chars[num];
        }


        System.Text.StringBuilder strBuilder;
        private void Server_Load(object sender, EventArgs e)
        {
            txtKey.Text = strkey;
            timer1.Start();
        }

        private void btnSN_Click(object sender, EventArgs e)
        {
            if (txtMessage.Text != string.Empty)
            {
                int n = txtMessage.Text.ToString().Length;
                Random rand = new Random();
                int num = rand.Next(0, n);
                strBuilder = new System.Text.StringBuilder(txtMessage.Text);
                strBuilder[num] = GetChar();
                string tmp = PaddingText(strBuilder.ToString());

                foreach (Socket item in clientlist)
                {
                    SendNoise(item, tmp, txtMessage.Text.Length);
                }
                DialogResult dlr = MessageBox.Show("Tin nhắn đã bị thay đổi. Bạn muốn thoát chương trình?",
                   "Thông báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dlr == DialogResult.Yes) Application.Exit();
                txtMessage.Clear();
            }

        }

        // RSA

        int time=0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (time == 60)
            {
                int key = int.Parse(strkey);
                key++;
                txtKey.Text = key.ToString();
                strkey = key.ToString();
                MessageBox.Show("Time Out",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                time = 0;
            }
            time++;
        }
    }
}
