using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace network_IAsyncSocketProgram
{
    public partial class server : Form
    {
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;
        Socket servSock;

        private List<string> createdServerNameList;
        private List<string> createdServerCodeList;

        List<Socket> connectedClients;

        // List<string> connectedUsersID;

        private bool mouseDown;
        private Point lastLocation;

        void AppendText(Control ctrl, string s)
        {
            if (ctrl.InvokeRequired) ctrl.Invoke(_textAppender, ctrl, s);
            else
            {
                string source = ctrl.Text;
                ctrl.Text = source + Environment.NewLine + s;
            }
        }

        //Server Init
        public server()
        {
            InitializeComponent();

            servSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _textAppender = new AppendTextDelegate(AppendText);
            connectedClients = new List<Socket>();

            createdServerNameList = new List<string>();
            createdServerCodeList = new List<string>();

            //connectedUsersID = new List<string>();

            IPEndPoint servEP = new IPEndPoint(IPAddress.Parse("192.168.1.9"),15000);
            servSock.Bind(servEP);
            servSock.Listen(10);

            AppendText(servMsgBox, string.Format("서버 시작 : " + servEP));

            /// 비동기적으로 클라이언트의 연결 요청을 받는다.
            servSock.BeginAccept(AcceptCallBack, null);
        }

        // MARK - 클라이언트 소켓의 연결을 비동기적으로 받는다.
        void AcceptCallBack(IAsyncResult ar)
        {
            Socket clientSock = servSock.EndAccept(ar);

            servSock.BeginAccept(AcceptCallBack, null);

            AsyncObject obj = new AsyncObject(5120);
            obj.workingSocket = clientSock;

            connectedClients.Add(clientSock);

            AppendText(servMsgBox, string.Format("클라이언트 " + clientSock.RemoteEndPoint +
                "가 연결되었습니다."));

            clientSock.BeginReceive(obj.buffer, 0, 5120, 0, DataReceived, obj);
        }

        // MARK - 클라이언트로부터 수신한 메세지 처리
        void DataReceived(IAsyncResult ar)
        {
            AsyncObject obj = (AsyncObject)ar.AsyncState;

            obj.workingSocket.EndReceive(ar);

            string text = Encoding.UTF8.GetString(obj.buffer).Trim('\0');

            string[] tokens = text.Split(':');

            /// MARK - 수신한 메세지가 보통 메세지일 경우
            if (tokens[0] == "send")
            {
                string user = tokens[2];
                string msg = tokens[3];

                AppendText(servMsgBox, string.Format("{0} : {1}", user, msg));

                for (int i = connectedClients.Count - 1; i >= 0; i--)
                {
                    Socket usersSock = connectedClients[i];

                    if (usersSock != obj.workingSocket)
                    {
                        usersSock.Send(obj.buffer);
                    }
                }

                obj.clearBuffer();

                obj.workingSocket.BeginReceive(obj.buffer, 0, 5120, 0, DataReceived, obj);
            }
            else if (tokens[0] == "exitInvolvedServer")
            {
                int serverIndex = int.Parse(tokens[3]);

                string serverCode = createdServerCodeList[serverIndex];

                byte[] b;

                for (int i = connectedClients.Count - 1; i >= 0; i--)
                {
                    Socket userSock = connectedClients[i];

                    if (userSock == obj.workingSocket)
                    {
                        b = Encoding.UTF8.GetBytes("responseCode" + ':' + serverCode.Trim());

                        userSock.Send(b);
                    }
                    else
                    {
                        b = Encoding.UTF8.GetBytes("exitInvolvedServer" + ':' + tokens[1] + ':' + serverCode + ':' + tokens[2] );

                        userSock.Send(b);
                    }
                }
                obj.clearBuffer();

                obj.workingSocket.BeginReceive(obj.buffer, 0, 5120, 0, DataReceived, obj);
            }
            /// MARK - 수신한 메세지가 종료 메세지일 경우
            else if (tokens[0] == "close")
            {

                for (int i = connectedClients.Count - 1; i >= 0; i--)
                {
                    Socket userSock = connectedClients[i];

                    if (userSock == obj.workingSocket)
                    {
                        userSock.Dispose();
                        connectedClients.RemoveAt(i);
                    }
                    else
                    {
                        /// 채팅 서버에 접속해있는 사용자 일 경우만 다른 클라이언트 전송합니다.
                        if(tokens.Length >= 3 )
                            userSock.Send(obj.buffer);
                    }
                }


                obj.clearBuffer();
            }

            /// MARK - 수신한 메세지가 채팅방 생성 메세지일 경우
            else if (tokens[0] == "createServer")
            {

                createdServerNameList.Add(tokens[1]);
                createdServerCodeList.Add(tokens[2]);

                AppendText(servMsgBox, string.Format("서버 {0}가 생성되었습니다.", tokens[1]));

                obj.clearBuffer();

                obj.workingSocket.BeginReceive(obj.buffer, 0, 4096, 0, DataReceived, obj);
            }
            /// MARK - 클라이언트가 선택한 채팅방의 코드를 요청 했을 때
            else if(tokens[0] == "requestServerCode")
            {
                int serverIndex = int.Parse(tokens[2]);

                string serverCode = createdServerCodeList[serverIndex];

                byte[] b;

                for (int i = connectedClients.Count - 1; i >= 0; i--)
                {
                    Socket userSock = connectedClients[i];

                    if (userSock == obj.workingSocket)
                    {
                        b = Encoding.UTF8.GetBytes("responseCode" + ':' + serverCode.Trim());

                        userSock.Send(b);
                    }
                    else
                    {
                        b = Encoding.UTF8.GetBytes("newUser" + ':' + tokens[1] + ':' + serverCode.Trim());

                        userSock.Send(b);
                    }
                }

                obj.clearBuffer();

                obj.workingSocket.BeginReceive(obj.buffer, 0, 4096, 0, DataReceived, obj);
            }
            /// MARK - 새로 접속한 유저에게 기존에 생성된 채팅방 목록을 전송
            else if (tokens[0] == "requestServerList")
            {
                string serverList = setCreatedServerList();

                byte[] b = Encoding.UTF8.GetBytes("responseServerList" + ':' + serverList);

                obj.workingSocket.Send(b);

                obj.clearBuffer();

                obj.workingSocket.BeginReceive(obj.buffer, 0, 4096, 0, DataReceived, obj);

            }
        }
        // -------------------------------------------------------------

        private string setCreatedServerList()
        {
            string serverList = null;

            for (int i = 0; i < createdServerNameList.Count; i++)
            {
                serverList += createdServerNameList[i];
                serverList += ",";
            }

            AppendText(servMsgBox, string.Format("{0}", serverList));

            return serverList;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // ----------------- 마우스로 폼 이동 ------------------------
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
        // -----------------------------------------------------------
    }
}
