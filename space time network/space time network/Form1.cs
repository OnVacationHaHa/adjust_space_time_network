using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace space_time_network
{
    public partial class Form1 : Form
    {
        read_file r;
        public Form1()
        {
            InitializeComponent();
        }
        private void 读取ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.Description = "请选择输入文件所在文件夹";
            string input_path;
            if (fb.ShowDialog() == DialogResult.OK)
            {
                input_path = fb.SelectedPath;
            }
            else
            {
                return;
            }
            r = new read_file(input_path);
            for (int i = 0; i < r.total_train_num; i++)
            {
                comboBox1.Items.Add(i + 1);
                comboBox2.Items.Add(i + 1);
            }
            comboBox1.SelectedIndex = 0;            
            for (int i = 0; i < r.station_num; i++)
            {
                comboBox3.Items.Add(i + 1);
            }
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
        }
        private void main(int pre_t,int lat_t,int change_station)
        {
            //r = new read_file("C:\\Users\\11413\\Desktop\\work\\2020-6-4\\single\\input_file");
            List<mat> mats = new List<mat>();
            int[][] paths = new int[r.total_train_num][];
            for (int i = 1; i <= r.total_train_num; i++)
            {
                mats.Add(new mat(r, i));
                mats[i - 1].init_mat();
            }
            init_orders(r);
            change_orders(pre_t, lat_t, change_station);
            while (orders.Count!=0)
            {
                int now_train = orders[0][0];
                int now_train_dir = r.dir[now_train - 1];
                get_next_path(now_train_dir, out now_train, out int start_station, out int end_station);                
                int[] now_path=mats[now_train - 1].change_OD_and_get_path(start_station, end_station,paths);
                if (r.is_double==false)
                {
                    for (int i = 1; i <= r.total_train_num; i++)
                    {
                        if (now_train == i)
                        {
                            continue;
                        }
                        bool same_dir = true;
                        if (r.dir[i - 1] == r.dir[now_train - 1])
                            same_dir = true;
                        else
                            same_dir = false;
                        mats[i - 1].remove_arc(now_path, same_dir);
                    }
                }
                else
                {
                    for (int i = 1; i <= r.total_train_num; i++)
                    {
                        if (now_train == i)
                        {
                            continue;
                        }
                        mats[i - 1].remove_arc(now_path);
                    }
                }
                int[] now_path_ = new int[now_path.Length - 2];
                for (int i = 1; i < now_path.Length-1; i++)
                {
                    now_path_[i - 1] = now_path[i];
                }
                if (paths[now_train-1]==null)
                {
                    paths[now_train - 1] = now_path_;
                }
                else
                {
                    paths[now_train - 1] = mat.合并数组(paths[now_train - 1],now_path_);
                }
            }
            DataTable node = mat.define_node();
            DataTable road_link = mat.define_road_link();
            DataTable agent = mat.define_agent();
            mats[0].init_node(node);
            int road_link_id = 1;
            for (int i = 0; i < paths.Length; i++)
            {
                mats[i].generate_nexta(paths[i],i+1,node, road_link, agent, ref road_link_id);
            }
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.Description = "请选择输出文件夹路径";
            string output_path;
            if (fb.ShowDialog() == DialogResult.OK)
            {
                output_path = fb.SelectedPath;
            }
            else
            {
                return;
            }
            SaveCsv(node, output_path+"\\node");
            SaveCsv(road_link, output_path+ "\\road_link");
            SaveCsv(agent, output_path+"\\agent");
            SaveCsv(r.agent_type, output_path+"\\agent_type");
            MessageBox.Show("保存成功，请打开NEXTA.exe查看");
        }
        private List<int[]> orders;
        private void init_orders(read_file r)
        {
            orders = new List<int[]>();
            for (int i = 1; i <= r.total_train_num; i++)
            {
                if (r.dir[i - 1] == 1)
                {
                    for (int j = 0; j < r.station_num - 1; j++)
                    {
                        int[] now_order = new int[2] { i, j + 1 };
                        orders.Add(now_order);
                    }
                }
                else
                {
                    for (int j = 0; j < r.station_num - 1; j++)
                    {
                        int[] now_order = new int[2] { i, r.station_num - j - 1 };
                        orders.Add(now_order);
                    }
                }
            }
        }
        private void change_orders(int pre_t, int lat_t, int station)
        {
            int pre_flag = r.dir[pre_t - 1];
            int lat_flag = r.dir[lat_t - 1];
            List<int[]> pre_block = new List<int[]>();
            List<int[]> lat_block = new List<int[]>();
            if (station != 1 && station != r.station_num)
            {
                if (pre_flag==1)
                {
                    pre_block.Add(new int[2] { pre_t, station - 1 });
                    pre_block.Add(new int[2] { pre_t, station });                    
                }                
                else
                {                    
                    pre_block.Add(new int[2] { pre_t, station });
                    pre_block.Add(new int[2] { pre_t, station - 1 });
                }
                if (lat_flag==1)
                {
                    for (int i = 1; i <= station;i++)
                    {
                        lat_block.Add(new int[2] { lat_t, i });
                    }
                }
                else
                {
                    for (int i = r.station_num-1; i >= station-1; i--)
                    {
                        lat_block.Add(new int[2] { lat_t, i });
                    }
                    lat_block.Add(new int[2] { lat_t, station });
                    lat_block.Add(new int[2] { lat_t, station - 1 });
                }
            }
            else if (station == 1)
            {
                pre_block.Add(new int[2] { pre_t, station });
                if (lat_flag==1)
                {
                    lat_block.Add(new int[2] { lat_t, station });
                }

                else
                {
                    for (int i = r.station_num-1; i >= 1; i--)
                    {
                        lat_block.Add(new int[2] { lat_t, i });
                    }
                }
            }
            else
            {
                pre_block.Add(new int[2] { pre_t, station - 1 });
                if (lat_flag==1)
                {
                    for (int i = 1; i <= r.station_num-1; i++)
                    {
                        lat_block.Add(new int[2] { lat_t, i});
                    }
                }
                else
                {
                    lat_block.Add(new int[2] { lat_t, station - 1 });
                }
                
            }            
            change_orders(pre_block, lat_block);
        }
        private void change_orders(List<int[]> pre, List<int[]> lat)
        {
            foreach (var l in lat)
            {
                orders_remove(l);
            }
            int pre_index = get_index(pre[0]);
            orders.InsertRange(pre_index, lat);
        }
        private int get_index(int[] block)
        {
            for (int i = 0; i < orders.Count; i++)
            {
                if (orders[i][0]==block[0] && orders[i][1]==block[1])
                {
                    return i;
                }
            }
            return -1;
        }
        private void orders_remove(int[] block)
        {
            for (int i = 0; i < orders.Count; i++)
            {
                if (orders[i][0] == block[0] && orders[i][1] == block[1])
                {
                    orders.RemoveAt(i);
                }
            }
        }
        private void get_next_path(int now_train_dir,out int train,out int start_station,out int end_station)
        {
            train = orders[0][0];
            int index=orders.Count-1;
            for (int i = 1; i < orders.Count; i++)
            {
                int now_train = orders[i][0];
                if (now_train!=train)
                {
                    index = i-1;break;
                }
            }
            int start_block = orders[0][1];
            int end_block = orders[index][1];
            if (now_train_dir==1)
            {
                start_station = start_block;
                end_station = end_block + 1;
            }
            else
            {
                start_station = start_block + 1;
                end_station = end_block;
            }
            for (int i = 0; i <= index; i++)
            {
                orders.RemoveAt(0);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            int pre_t = (int)comboBox2.SelectedItem;
            int lat_t = (int)comboBox1.SelectedItem;
            if (pre_t > lat_t)
            {
                MessageBox.Show("被越行列车应为高等级列车，否则无需设置越行规则！"); return;
            }
            else if (pre_t == lat_t)
            {
                MessageBox.Show("请输入不同车号！"); return;
            }
            int station = (int)comboBox3.SelectedItem;
            try
            {
                main(pre_t, lat_t, station);
            }
            catch (Exception)
            {
                MessageBox.Show("出现错误，可能是设定的最长停站时间不足");
            }
            
        }
        private void SaveCsv(DataTable dt, string filePath)
        {
            FileStream fs = null;
            StreamWriter sw = null;
            try
            {
                fs = new FileStream(filePath + dt.TableName + ".csv", FileMode.Create, FileAccess.Write);
                sw = new StreamWriter(fs, Encoding.Default);
                var data = string.Empty;
                //写出列名称
                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    data += dt.Columns[i].ColumnName;
                    if (i < dt.Columns.Count - 1)
                    {
                        data += ",";
                    }
                }
                sw.WriteLine(data);
                //写出各行数据
                for (var i = 0; i < dt.Rows.Count; i++)
                {
                    data = string.Empty;
                    for (var j = 0; j < dt.Columns.Count; j++)
                    {
                        data += dt.Rows[i][j].ToString();
                        if (j < dt.Columns.Count - 1)
                        {
                            data += ",";
                        }
                    }
                    sw.WriteLine(data);
                }
            }
            catch (IOException ex)
            {
                throw new IOException(ex.Message, ex);
            }
            finally
            {
                if (sw != null) sw.Close();
                if (fs != null) fs.Close();
            }
        }
    }
}
