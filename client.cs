using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.PerformanceData;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace network_IAsyncSocketProgram_client
{
    public partial class client : Form
    {
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;
        Socket clientSock;

        List<string> createdServerNameList = new List<string>();

        // MARK - 참여한 채팅 서버의 이름, 코드
        private string involvedServerName = null;
        private string involvedServerCode = null;
        private bool involvedServer = false;

        //List<string> connectedUsersID;
        string userID;

        private bool mouseDown;
        private Point lastLocation;

        // MARK - 클라이언트 Initiate
        public client()
        {
            InitializeComponent();

            clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _textAppender = new AppendTextDelegate(AppendText);

            sendMsgButton.Enabled = false;
        }

        void AppendText(Control ctrl, string s)
        {
            if (ctrl.InvokeRequired) ctrl.Invoke(_textAppender, ctrl, s);
            else
            {
                string source = ctrl.Text;
                ctrl.Text = source + Environment.NewLine + s;
            }
        }

        // MARK --------------  마우스로 폼 이동 -----------
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if(mouseDown)
            {
                this.Location = new Point(
                        (this.Location.X - lastLocation.X) + e.X,
                        (this.Location.Y - lastLocation.Y) + e.Y
                        );

                this.Update();
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }
        // ---------------------------------------------------------------

        // MARK - 서버와의 연결 종료
        private void button1_Click(object sender, EventArgs e)
        {
            string closeTxT = "";

            if (involvedServerCode != null)
            {
                closeTxT = string.Format("close" + ':' + involvedServerCode + ':' + userID + "의 연결이 종료되었습니다.");
            }
            else
            {
                closeTxT = string.Format("close" + ':' + userID + "의 연결이 종료되었습니다.");
            }

            byte[] b = Encoding.UTF8.GetBytes(closeTxT);

            clientSock.Send(b);

            /// 종료

            Application.Exit();
        }

        private void client_Load(object sender, EventArgs e)
        {
            /// Mark - 서버 연결
            try
            {
                clientSock.Connect(IPAddress.Parse("192.168.1.9"), 15000);
            }
            catch (Exception ex)
            {
                AppendText(txtBox, string.Format("서버 연결 실패 : " + ex.ToString()));
                return;
            }

            AppendText(txtBox, string.Format("서버 연결 성공 :" + IPAddress.Loopback.ToString()));

            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = clientSock;
            clientSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
        }

        // MARK - 서버로부터 메세지 수신
        void DataReceived(IAsyncResult ar)
        {
            AsyncObject obj = (AsyncObject)ar.AsyncState;

            obj.WorkingSocket.EndReceive(ar);

            string text = Encoding.UTF8.GetString(obj.Buffer).Trim('\0');

            string[] tokens = text.Split(':');

            /// MARK - 일반 메세지 수신
            if(tokens[0] == "send")
            {
                string id = tokens[2];
                string msg = tokens[3];

                if (involvedServerCode == tokens[1])
                {
                    AppendText(txtBox, string.Format("[수신] {0} : {1} ", id, msg));
                }

            }
            else if(tokens[0] == "newUser")
            {
                if (tokens[2] == involvedServerCode)
                {
                    AppendText(txtBox, string.Format("{0} 님이 참여하셨습니다.", tokens[1]));
                }
            }
            else if (tokens[0] == "exitInvolvedServer")
            {
                if (involvedServerCode == tokens[1])
                {
                    AppendText(txtBox, string.Format("{0}님이 퇴장하셨습니다.", tokens[3]));
                }
                else if (involvedServerCode == tokens[2])
                {
                    AppendText(txtBox, string.Format("{0}님이 참여하셨습니다.", tokens[3]));
                }
            }
            /// MARK - 다른 클라이언트의 종료
            else if(tokens[0] == "close")
            {
                if (involvedServerCode == tokens[1])
                {
                    AppendText(txtBox, string.Format("{0}", tokens[2]));
                }
            }
            /// MARK - 접속하고자 하는 채팅 서버의 코드를 수신
            else if (tokens[0] == "responseCode")
            {
                involvedServerCode = tokens[1];
                sendMsgButton.Enabled = true;
            }
            /// MARK - 생성된 채팅 서버 목록 수신
            else if (tokens[0] == "responseServerList")
            {
                string[] serverList = tokens[1].Split(',');

                for (int i = 0; i < serverList.Length; i++)
                {
                    createdServerNameList.Add(serverList[i]);
                }
            }

            obj.ClearBuffer();
            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, SocketFlags.None, DataReceived, obj);

        }

        // MARK - 메세지 송신 버튼
        private void sendMsgButton_Click(object sender, EventArgs e)
        {
            string text = sendMsgBox.Text.Trim();

            byte[] b = Encoding.UTF8.GetBytes("send" + ':' + involvedServerCode + ':'  + userID + ':' + text);

            clientSock.Send(b);

            AppendText(txtBox, string.Format("[보냄] {0} : {1}", userID, text));

            sendMsgBox.Clear();

        }

        // MARK - 클라이언트의 서버 생성
        private void button2_Click(object sender, EventArgs e)
        {
            string createServerName = "";
            string createServerCode = "";

            if(userName.Text == "Input User Name" || userName.Text == null)
            {
                MsgBoxHelper.Warn("사용하실 이름을 입력해주세요");
                userName.Focus();
            }

            /// MARK - 서버 생성 폼으로부터, 서버 명 - 서버 코드 수신
            else
            {
                userID = userName.Text.Trim();

                createChattingServer createServer = new createChattingServer();

                if (createServer.ShowDialog() == DialogResult.OK)
                {
                    createServerName = createServer.returnServerName;
                    createServerCode = createServer.returnServerCode;

                    byte[] b = Encoding.UTF8.GetBytes("createServer" + ':' + createServerName + ':' +
                                                      createServerCode);
                    clientSock.Send(b);

                    createdServerNameList.Add(createServerName);

                    createdServerList.Items.Add(createServerName);
                }
            }

          
        }

        // MARK - 서버 목록 새로고침
        private void refresh_Click(object sender, EventArgs e)
        {
            byte[] b = Encoding.UTF8.GetBytes("requestServerList" + ':');

            createdServerNameList.Clear();
            createdServerList.Items.Clear();

            clientSock.Send(b);

            while (true)
            {
                if (createdServerNameList.Count >= 1)
                    break;
            }

            for (int i = 0; i < createdServerNameList.Count; i++)
            {
                createdServerList.Items.Add(createdServerNameList[i]);
            }
        }

        // MARK - 접속하고자 하는 채팅을 더블클릭 했을 때
        private void createdServerList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = createdServerList.IndexFromPoint(e.Location);
            userID = userName.Text;

            if (userName.Text == "Input User Name" || userName.Text == null)
            {
                MsgBoxHelper.Warn("닉네임을 먼저 입력하세요.");
                userName.Focus();
            }
            else
            {
                if (createdServerNameList[index] != involvedServerName)
                {
                    if (index != ListBox.NoMatches)
                    {
                        txtBox.Clear();

                        involvedServerName = createdServerNameList[index];
                        serverName.Text = involvedServerName;

                        if (involvedServerCode != null)
                        {
                            byte[] b = Encoding.UTF8.GetBytes("exitInvolvedServer" + ':' + involvedServerCode + ':' + userID + ':' + index);

                            clientSock.Send(b);
                        }
                        else
                        {
                            byte[] b = Encoding.UTF8.GetBytes("requestServerCode" + ':' + userID + ':' + index);

                            clientSock.Send(b);

                        }

                        AppendText(txtBox, string.Format("채팅방 {0}에 접속하였습니다.", createdServerNameList[index]));

                        sendMsgButton.Enabled = true;
                    }
                }
                else
                {
                    MsgBoxHelper.Warn("이미 접속한 채팅 서버 입니다.");
                }
            }
        }


        private void sendMsgBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string text = sendMsgBox.Text.Trim();

                byte[] b = Encoding.UTF8.GetBytes("send" + ':' + involvedServerCode + ':' + userID + ':' + text);

                clientSock.Send(b);

                AppendText(txtBox, string.Format("[보냄] {0} : {1}", userID, text));

                sendMsgBox.Clear();
            }
            else
            {
                return;
            }
        }
    }
}
