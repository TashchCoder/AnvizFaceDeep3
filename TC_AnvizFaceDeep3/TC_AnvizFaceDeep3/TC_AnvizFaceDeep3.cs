using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnvizDemo;

namespace TC_AnvizFaceDeep3
{
    public class TC_AnvizFaceDeep3
    {
        #region global vars

        //структура для получения пользователя
        public struct Person
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public bool PictureStatus { get; set; } // 1 - yes, 0 - no picture

            public int CardNum { get; set; }



        }

        //структура, хранящая сетевые настройки
        public struct NetConfig
        {
            public byte[] IP { get; set; }
            public byte[] Mask { get; set; }
            public byte[] Gateway { get; set; }

        }


        //объект, через который реализуется функционал библиотеки Anviz.
        IntPtr anviz_handle;

        //для отслеживания методами Connect и Disconnect ответов на свои команды
        bool isConnect;

        //индекс устройства, задаётся при подключении, используется при последующих командах
        int DevIdx;

        //добавить нового пользователя
        bool Answer8;

        //удалить пользователя
        bool Answer31;


        //получить пользователя
        bool Answer88;

        //сюда приходят параметры пользователя по вызову метода GetPerson
        Person gettingPerson;


        //загрузить в считыватель фото
        bool Answer160;

        //индикатор получения первого сообщения типа 19
        bool FirstAnswer19IsGet;

        //событие для обработчика записей
        public Action<int, DateTime> Handler;

        //индикатор получения сетевых настроек
        bool Answer23;

        //индикатор первого получения сетевых настроек
        bool Answer23First;

        //сюда приходят сетевые настройки
        Anviz.CCHEX_NETCFG_INFO_STRU netConf;

        //индикатор установки сетевых настроек
        bool Answer24;





        #endregion


        #region constructors

        public TC_AnvizFaceDeep3()
        {
            Anviz.CChex_Init();
            anviz_handle = Anviz.CChex_Start();
            isConnect = false;
            Answer8 = false;
            Answer31 = false;
            Answer88 = false;
            Answer160 = false;
            FirstAnswer19IsGet = false;
            Answer23 = false;
            Answer23First = false;
            Answer24 = false;
            
            UpdateTread();



        }

        #endregion


        #region Update

        private void UpdateTread()
        {
            Thread uptr = new Thread(UpdateCycle);
            uptr.Name = "UpdateThread";
            uptr.IsBackground = true;
            uptr.Start();


        }


        private void UpdateCycle()
        {
            while (true)
            {
                Update();
                Thread.Sleep(100);
            }


        }



        private void Update()
        {
            //сюда запишется результат запроса
            int ret = 0;
            //сюда запишется тип ответа
            int[] Type = new int[1];
            //номер ответившего устройства
            int[] dev_idx = new int[1];
            //буфер с ответом
            IntPtr pBuff;

            int len = 32000;
            pBuff = Marshal.AllocHGlobal(len);

            if (anviz_handle == IntPtr.Zero)
            {
                return;
            }

            ret = Anviz.CChex_Update(anviz_handle, dev_idx, Type, pBuff, len);

            //если пришёл адекватный ответ
            if (ret > 0)
            {
                Console.WriteLine("Update:  " + Type[0]);

                switch (Type[0])
                {
                    //CCHex_ClientConnect
                    case (int)Anviz.MsgType.CCHEX_RET_DEV_LOGIN_TYPE: //=2
                        {
                            Anviz.CCHEX_RET_DEV_LOGIN_STRU dev_info;
                            dev_info = (Anviz.CCHEX_RET_DEV_LOGIN_STRU)Marshal.PtrToStructure(pBuff, typeof(Anviz.CCHEX_RET_DEV_LOGIN_STRU));

                            DevIdx = dev_info.DevIdx;
                            isConnect = true;

                            Anviz.CChex_GetNetConfig(anviz_handle, DevIdx);

                            break;
                        }

                    //CCHex_ClientDisconnect
                    case (int)Anviz.MsgType.CCHEX_RET_DEV_LOGOUT_TYPE: //=3
                        {
                            Anviz.CCHEX_RET_DEV_LOGOUT_STRU dev_info;
                            dev_info = (Anviz.CCHEX_RET_DEV_LOGOUT_STRU)Marshal.PtrToStructure(pBuff, typeof(Anviz.CCHEX_RET_DEV_LOGOUT_STRU));

                            DevIdx = -1;
                            isConnect = false;

                            break;
                        }

                    //CChex_ModifyPersonInfo
                    case (int)Anviz.MsgType.CCHEX_RET_ULEMPLOYEE2_UNICODE_INFO_TYPE: //8
                        {

                            Anviz.CCHEX_RET_DEL_EMPLOYEE_INFO_STRU result;
                            result = (Anviz.CCHEX_RET_DEL_EMPLOYEE_INFO_STRU)Marshal.PtrToStructure(pBuff, typeof(Anviz.CCHEX_RET_DEL_EMPLOYEE_INFO_STRU));

                            if ((result.Result == 0) && (!Answer8))
                            {
                                Answer8 = true;
                            }
                            else
                            {
                                Answer8 = false;
                            }



                            break;
                        }

                    //2 ответ на CCHex_ClientConnect и сообщение о новой записи
                    case (int)Anviz.MsgType.CCHEX_RET_DEV_STATUS_TYPE: //19
                        {
                            if (!FirstAnswer19IsGet)
                            {
                                FirstAnswer19IsGet = true;

                                Anviz.CCHEX_DEL_RECORD_INFO_STRU delete_record;

                                delete_record.del_type = 0;// delete all record;
                                delete_record.del_count = 0; // skip

                                Anviz.CChex_DeleteRecordInfo(anviz_handle, DevIdx, ref delete_record);


                            }
                            else
                            {
                                Anviz.CChex_DownloadAllRecords(anviz_handle, DevIdx);


                            }



                            break;
                        }


                    case (int)Anviz.MsgType.CCHEX_RET_GETNETCFG_TYPE: //23
                        {

                            Anviz.CCHEX_RET_GETNETCFG_STRU status;
                            status = (Anviz.CCHEX_RET_GETNETCFG_STRU)Marshal.PtrToStructure(pBuff, typeof(Anviz.CCHEX_RET_GETNETCFG_STRU));

                            netConf.IpAddr = new byte[4];
                            for (int i = 0; i < netConf.IpAddr.Length; i++)
                            {
                                netConf.IpAddr[i] = status.Cfg.IpAddr[i];
                            }

                            netConf.IpMask = new byte[4];
                            for (int i = 0; i < netConf.IpAddr.Length; i++)
                            {
                                netConf.IpMask[i] = status.Cfg.IpMask[i];
                            }

                            netConf.GwAddr = new byte[4];
                            for (int i = 0; i < netConf.IpAddr.Length; i++)
                            {
                                netConf.GwAddr[i] = status.Cfg.GwAddr[i];
                            }

                            netConf.MacAddr = new byte[6];
                            for (int i = 0; i < netConf.MacAddr.Length; i++)
                            {
                                netConf.MacAddr[i] = status.Cfg.MacAddr[i];
                            }

                            netConf.ServAddr = new byte[4];
                            for (int i = 0; i < netConf.ServAddr.Length; i++)
                            {
                                netConf.ServAddr[i] = status.Cfg.ServAddr[i];
                            }

                            netConf.RemoteEnable = status.Cfg.RemoteEnable;

                            netConf.Port = new byte[2];
                            for (int i = 0; i < netConf.Port.Length; i++)
                            {
                                netConf.Port[i] = status.Cfg.Port[i];
                            }
                            netConf.Mode = status.Cfg.Mode;
                            netConf.DhcpEnable = status.Cfg.DhcpEnable;





                            //netConf = status.Cfg;

                            if (!Answer23First)
                            {
                                Answer23First = true;
                            }
                            else
                            {
                                Answer23 = true;
                            }




                            break;
                        }



                    case (int)Anviz.MsgType.CCHEX_RET_SETNETCFG_TYPE: //24
                        {


                            Anviz.CCHEX_RET_SETNETCFG_STRU status;
                            status = (Anviz.CCHEX_RET_SETNETCFG_STRU)Marshal.PtrToStructure(pBuff, typeof(Anviz.CCHEX_RET_SETNETCFG_STRU));

                            if (status.Result == 0)
                            {
                                Answer24 = true;
                            }


                            break;
                        }



                    //CChex_DeletePersonInfo
                    case (int)Anviz.MsgType.CCHEX_RET_DEL_PERSON_INFO_TYPE: //31
                        {

                            Anviz.CCHEX_RET_DEL_EMPLOYEE_INFO_STRU answer;
                            answer = (Anviz.CCHEX_RET_DEL_EMPLOYEE_INFO_STRU)Marshal.PtrToStructure(pBuff, typeof(Anviz.CCHEX_RET_DEL_EMPLOYEE_INFO_STRU));

                            if ((answer.Result == 0) && (!Answer31))
                            {
                                Answer31 = true;
                            }
                            else
                            {
                                Answer31 = false;
                            }

                            break;
                        }

                    //CChex_GetOnePersonInfo
                    case (int)Anviz.MsgType.CCHEX_RET_GET_ONE_EMPLOYEE_INFO_TYPE: //88
                        {

                            Anviz.CCHEX_RET_PERSON_INFO_STRU status;
                            status = (Anviz.CCHEX_RET_PERSON_INFO_STRU)Marshal.PtrToStructure(pBuff, typeof(Anviz.CCHEX_RET_PERSON_INFO_STRU));
                            gettingPerson.ID = ByteArrToNumber(status.EmployeeId);
                            gettingPerson.Name = byte_to_unicode_string(status.EmployeeName);
                            gettingPerson.PictureStatus = (status.Fp_Status == 1024);
                            gettingPerson.CardNum = (int)status.card_id;

                            if (!Answer88)
                            {
                                Answer88 = true;
                            }
                            else
                            {

                            }
                            break;
                        }


                    case (int)Anviz.MsgType.CCHEX_RET_TM_ALL_RECORD_INFO_TYPE: //150
                        {


                            Anviz.CCHEX_RET_TM_RECORD_INFO_STRU record_info;
                            record_info = (Anviz.CCHEX_RET_TM_RECORD_INFO_STRU)Marshal.PtrToStructure(pBuff, typeof(Anviz.CCHEX_RET_TM_RECORD_INFO_STRU));
                            DateTime date = new DateTime(2000, 1, 2).AddSeconds(swapInt32(BitConverter.ToUInt32(record_info.Date, 0)));
                            string dateStr = date.ToString("yyyy-MM-dd HH:mm:ss");

                            //ToDo: тут обработчик записи
                            //Console.WriteLine("Date: " + dateStr + "; ID=" + ByteArrToNumber(record_info.EmployeeId));
                            if (Handler != null)
                            {
                                
                                Handler?.Invoke(ByteArrToNumber(record_info.EmployeeId), date);

                            }



                            Anviz.CCHEX_DEL_RECORD_INFO_STRU delete_record;
                            delete_record.del_type = 0;// delete all record;
                            delete_record.del_count = 0; // skip
                            Anviz.CChex_DeleteRecordInfo(anviz_handle, DevIdx, ref delete_record);



                            break;
                        }

                    //CChex_UploadGetFacePictureModule
                    case 160:
                        {
                            if (!Answer160)
                            {
                                Answer160 = true;
                            }
                            else
                            {

                            }

                            break;
                        }






                    default:
                        break;
                }


            }





        }




        #endregion


        #region public methods



        async Task<bool> ConnectAsync(string ip)
        {
            bool result = await Task.Run( ()=>Connect(ip) );

            return result;

        }

        public bool Connect(string ip)
        {



            int port = Anviz.CChex_Get_Service_Port(anviz_handle);
            byte[] Ipstr = new byte[16];
            Ipstr = System.Text.Encoding.Default.GetBytes(ip);

            int res = Anviz.CCHex_ClientConnect(anviz_handle, Ipstr, port);

            for (int i = 0; i < 50; i++)
            {
                if (isConnect)
                {
                    return true;
                }
                else
                {
                    Thread.Sleep(100);
                }


            }

            return false;


        }

        async Task<bool> DisconnectAsync()
        {
            bool result = await Task.Run( ()=>Disconnect() );


            return result;

        }

        public bool Disconnect()
        {
            if (!isConnect && DevIdx == -1)
            {
                return true;
            }

            if (DevIdx != -1)
            {
                int res = Anviz.CCHex_ClientDisconnect(anviz_handle, DevIdx);
            }


            for (int i = 0; i < 50; i++)
            {
                if (!isConnect)
                {
                    DevIdx = -1;
                    FirstAnswer19IsGet = false;
                    return true;
                }
                else
                {
                    Thread.Sleep(100);
                }


            }

            return false;

        }


       async Task<bool> AddNewPersonAsync(int PersonID, string PersonName, int CardNum)
        {
            bool result = await Task.Run(() => AddNewPerson(PersonID, PersonName, CardNum));


            return result;

        }
        public bool AddNewPerson(int PersonID, string PersonName, int CardNum)
        {
            Anviz.CCHEX_RET_PERSON_INFO_STRU person = new Anviz.CCHEX_RET_PERSON_INFO_STRU();

            person.EmployeeId = NumberToByteArr(PersonID);

            person.EmployeeName = new byte[64];
            byte[] name = Encoding.Unicode.GetBytes(PersonName);
            for (int i = 0; i < 64; i += 2)
            {
                if (i < name.Length)
                {
                    person.EmployeeName[i] = name[i + 1];
                    person.EmployeeName[i + 1] = name[i];
                    continue;
                }
                person.EmployeeName[i] = 0;
            }
            //Array.Copy(name, person.EmployeeName, name.Length);
            person.password = 0xFFFFF;
            person.card_id = (uint)CardNum;
            person.DepartmentId = 0;
            person.GroupId = 1;
            person.Mode = 6;
            person.Fp_Status = 1024;
            person.Special = 64;
            person.Rserved1 = 0x00;  // do not modify
            person.Rserved2 = 0;
            int res = Anviz.CChex_ModifyPersonInfo(anviz_handle, DevIdx, ref person, 1);

            for (int i = 0; i < 50; i++)
            {
                if (Answer8)
                {
                    Answer8 = false;
                    return true;
                }
                else
                {
                    Thread.Sleep(100);
                }


            }
            return false;

        }



        async Task<bool> DeletePersonAsync(int PersonID)
        {
            bool result = await Task.Run(() => DeletePerson(PersonID));


            return result;

        }
        public bool DeletePerson(int PersonID)
        {
            Anviz.CCHEX_DEL_PERSON_INFO_STRU person = new Anviz.CCHEX_DEL_PERSON_INFO_STRU();
            person.EmployeeId = NumberToByteArr(PersonID);
            person.operation = 0xFF;
            int res = Anviz.CChex_DeletePersonInfo(anviz_handle, DevIdx, ref person);

            for (int i = 0; i < 50; i++)
            {
                if (Answer31)
                {
                    Answer31 = false;
                    return true;
                }
                else
                {
                    Thread.Sleep(100);
                }


            }

            return false;

        }


        async Task<Person> GetPersonAsync(int ID)
        {
            Person person = await Task.Run(() => GetPerson(ID));


            return person;

        }


        public Person GetPerson(int ID)
        {
            Person person = new Person();

            Anviz.CCHEX_GET_ONE_EMPLOYEE_INFO_STRU Employee;
            Employee.EmployeeId = NumberToByteArr(ID);
            int ret = Anviz.CChex_GetOnePersonInfo(anviz_handle, DevIdx, ref Employee);


            for (int i = 0; i < 50; i++)
            {
                if (Answer88)
                {
                    person.ID = gettingPerson.ID;
                    person.Name = gettingPerson.Name;
                    person.PictureStatus = gettingPerson.PictureStatus;
                    person.CardNum = gettingPerson.CardNum;
                    return person;
                }
                else
                {
                    Thread.Sleep(100);
                }


            }

            return person;


        }




        async Task<bool> UploadPictureAsync(int ID, byte[] Buff, int Len)
        {
            bool result = await Task.Run(() => UploadPicture(ID, Buff, Len));


            return result;

        }


        public bool UploadPicture(int ID, byte[] Buff, int Len)
        {
            Anviz.CCHEX_DEL_PERSON_INFO_STRU Data;
            Data.EmployeeId = NumberToByteArr(ID);
            Data.operation = 11;


            int ret = Anviz.CChex_UploadGetFacePictureModule(anviz_handle, DevIdx, ref Data, Buff, Len);

            for (int i = 0; i < 50; i++)
            {
                if (Answer160)
                {
                    Answer160 = false;
                    return true;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }

            return false;


        }



        async Task<bool> GetNetConfigAsync(byte[] IP, byte[] Mask, byte[] Gateway)
        {
            bool result = await Task.Run(() => GetNetConfig(IP, Mask, Gateway));


            return result;

        }

        public bool GetNetConfig(byte[] IP, byte[] Mask, byte[] Gateway)
        {

            Anviz.CChex_GetNetConfig(anviz_handle, DevIdx);
            for (int i = 0; i < 50; i++)
            {
                if (Answer23)
                {
                    Answer23 = false;

                    for (int i1 = 0; i1 < IP.Length; i1++)
                    {
                        IP[i1] = netConf.IpAddr[i1];

                    }

                    for (int i2 = 0; i2 < IP.Length; i2++)
                    {
                        Mask[i2] = netConf.IpMask[i2];

                    }

                    for (int i3 = 0; i3 < IP.Length; i3++)
                    {
                        Gateway[i3] = netConf.GwAddr[i3];

                    }

                    //IP = netConf.IpAddr;
                    //Mask = netConf.IpMask;
                    //Gateway = netConf.GwAddr;


                    return true;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            IP = new byte[4];
            Mask = new byte[4]; ;
            Gateway = new byte[4];

            return false;

        }


        async Task<bool> SetNetConfigAsync(byte[] IP, byte[] Mask, byte[] Gateway)
        {
            bool result = await Task.Run(() => SetNetConfig(IP, Mask, Gateway));


            return result;

        }


        public bool SetNetConfig(byte[] IP, byte[] Mask, byte[] Gateway)
        {

            netConf.IpAddr = IP;
            netConf.IpMask = Mask;
            netConf.GwAddr = Gateway;

            Anviz.CChex_SetNetConfig(anviz_handle, DevIdx, ref netConf);

            for (int i = 0; i < 50; i++)
            {
                if (Answer24)
                {
                    Answer24 = false;

                    return true;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }

            return false;


        }





        #endregion


        #region private methods

        // массив байт в номер, EmployeeID
        private static int ByteArrToNumber(byte[] arr)
        {
            //if (arr.Length == 1)
            //{
            //    return (int)arr[0];
            //}
            //else
            //{
            //    return (((int)arr[3] << 8) | ((int)arr[4]));
            //}
            int res = 0;
            for (int i = 0; i < arr.Length; i++)
            {

                res = res | ((int)arr[i] << (4 - i) * 8);
            }
            return res;

        }

        //номер в массив байт, обратное действие методу ByteArrToNumber 
        private static byte[] NumberToByteArr(int number)
        {
            byte[] res = new byte[5];



            for (int i = 0; i < res.Length; i++)
            {
                res[i] = (byte)((number & (255 << 8 * (res.Length - (1 + i)))) >> 8 * (res.Length - (1 + i)));
            }
            res[0] = 0;


            return res;

        }


        // массив байт в строку посимвольно
        private string ByteArr_to_String(byte[] StringData)
        {
            return Encoding.Default.GetString(StringData).Replace("\0", "");
        }


        private string byte_to_unicode_string(byte[] StrData)
        {
            //log_add_string(Encoding.BigEndianUnicode.GetString(StringData));
            int i;
            byte[] StringData = new byte[StrData.Length];

            for (i = 0; i + 1 < StringData.Length; i += 2)
            {
                StringData[i] = StrData[i + 1];
                StringData[i + 1] = StrData[i];
            }
            return Encoding.Unicode.GetString(StringData).Replace("\0", "");
            //return Encoding.UTF8.GetString(StringData).Replace("\0", "");
        }


        private uint swapInt32(uint value)
        {
            return ((value & 0x000000FF) << 24) |
           ((value & 0x0000FF00) << 8) |
           ((value & 0x00FF0000) >> 8) |
           ((value & 0xFF000000) >> 24);
        }

        #endregion
    }
}
