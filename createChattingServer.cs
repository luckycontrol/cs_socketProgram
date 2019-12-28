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

namespace network_IAsyncSocketProgram_client
{
    public partial class createChattingServer : Form
    {

        // 유저ID, 서버 명
        private string serverName;
        private string serverCode;

        // 화면 제어 변수들
        private bool mouseDown;
        private Point lastLocation;

        // 생성자
        public createChattingServer()
        {
            InitializeComponent();
        }

        // 종료버튼
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // 서버 생성 버튼을 눌렀을 때
        private void button2_Click(object sender, EventArgs e)
        {
            if(createServerName.Text == null || createServerName.Text.Length < 3)
            {
                MsgBoxHelper.Warn("서버 명은 3글자 이상으로 해주세요");
                createServerName.Focus();
            }
            else if (createServerName.Text == "Input server name" || createServerName.Text == "input server name")
            {
                MsgBoxHelper.Warn("생성하실 서버 명을 입력해주세요.");
                createServerName.Focus();
            }
            else
            {
                Random r = new Random();
                DialogResult = DialogResult.OK;

                serverName = createServerName.Text.Trim();
                serverCode = r.Next(1, 1000).ToString().Trim();

                Close();
            }
            
        }

        public string returnServerName
        {
            get { return serverName; }
        }

        public string returnServerCode
        {
            get { return serverCode; }
        }

        // ------------------- 마우스로 창 움직이기 ------------------
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
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
        // -------------------------------------------------------------
    }
}
