using System.Collections.Generic;
using System.Data;

namespace space_time_network
{
    class mat
    {
        public int[,] s_t_mat { get; }
        //变量
        private int time_len, node_num, train_num;
        private int[] station_range;
        private read_file r;


        public mat(read_file r, int train_num)
        {
            this.train_num = train_num;
            this.r = r;
            int flag = r.dir[train_num - 1];           
            station_range = new int[r.station_num];
            if (flag == 1)
            {
                for (int i = 1; i <= r.station_num; i++)
                {
                    station_range[i - 1] = i;
                }
            }
            else
            {
                for (int i = 1; i <= r.station_num; i++)
                {
                    station_range[i - 1] = r.station_num - i + 1;
                }
            }
            time_len = time_sub2min(r.end_time, r.start_time) + 1;
            node_num = ((r.station_num - 2) * 3 + 2) * time_len + 2;//1 start_point 2 end_point
            s_t_mat = new int[node_num, node_num];
            for (int i = 0; i < node_num; i++)
            {
                for (int j = 0; j < node_num; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    else
                    {
                        s_t_mat[i, j] = int.MaxValue;
                    }
                }
            }
        }
        public int[] change_OD_and_get_path(int o_station, int d_station, int[][] paths)
        {
            for (int i = 0; i < node_num; i++)
            {
                s_t_mat[0, i] = int.MaxValue;
                s_t_mat[i, 1] = int.MaxValue;
            }
            s_t_mat[0, 0] = 0; s_t_mat[1, 1] = 0;
            if (paths[train_num-1]==null)
            {
                for (int i = 1; i <= time_len; i++)
                {
                    int now_node  = station_time2node_num(o_station, 2, i);
                    s_t_mat[0, now_node - 1] = i;
                }
                for (int i = 1; i <= time_len; i++)
                {
                    int now_node = 0;
                    if (r.stop_seq[train_num - 1].Exists(n => n == d_station) == false)
                    {
                        now_node = station_time2node_num(d_station, 3, i);
                    }
                    else
                    {
                        now_node = station_time2node_num(d_station, 1, i);
                    }
                    s_t_mat[now_node - 1, 1] = 0;
                }
            }
            else
            {
                int now_node_num = paths[train_num - 1][paths[train_num - 1].Length - 1];
                s_t_mat[0, now_node_num -1] = 0;
                for (int i = 1; i <= time_len; i++)
                {
                    int now_node = station_time2node_num(d_station, 1, i);
                    s_t_mat[now_node - 1, 1] = 0;
                }
            }                      
            return Dijkstra(1, 2);
        }
        public void init_mat()
        {
            int ear_time =int.Parse((string)r.departure_time_range.Rows[train_num - 1][1]);
            int lat_time = int.Parse((string)r.departure_time_range.Rows[train_num - 1][2]);
            ear_time = time_sub2min(ear_time, r.start_time)+1;
            lat_time = time_sub2min(lat_time, r.start_time)+1;
            for (int t = ear_time; t <= lat_time; t++)
            {
                int now_node = station_time2node_num(station_range[0], 0, t);
                int now_node2 = station_time2node_num(station_range[r.station_num - 1], 0, t);
                s_t_mat[0, now_node - 1] = t;
                s_t_mat[now_node2 - 1, 1] = 0;
            }//连接逻辑起点、逻辑终点
            for (int i = 0; i < r.station_num - 1; i++)
            {
                int now_station = station_range[i];
                int next_station = station_range[i + 1];
                List<int> now_state_range = new List<int>();
                List<int> next_state_range = new List<int>();
                //判断state
                if (now_station == station_range[0])
                {
                    now_state_range.Add(2);
                }
                else if (r.stop_seq[train_num - 1].Exists(t => t == now_station))
                {
                    now_state_range.Add(2);
                }
                else
                {
                    now_state_range.Add(2); now_state_range.Add(3);
                }
                if (r.stop_seq[train_num - 1].Exists(t => t == next_station))
                {
                    next_state_range.Add(1);
                }
                else if (next_station == station_range[r.station_num - 1])
                {
                    next_state_range.Add(1);
                }
                else
                {
                    next_state_range.Add(1); next_state_range.Add(3);
                }
                int running_time0, running_time;
                if (station_range[0] == 1)
                {
                    running_time0 = int.Parse((string)r.running_time.Rows[train_num - 1][now_station]);
                }
                else
                {
                    running_time0 = int.Parse((string)r.running_time.Rows[train_num - 1][now_station - 1]);
                }
                for (int j = 0; j < now_state_range.Count; j++)
                {
                    running_time = running_time0;
                    if (now_state_range[j] == 2)
                    {
                        running_time += r.add_start;
                    }
                    for (int jj = 0; jj < next_state_range.Count; jj++)
                    {
                        if (next_state_range[jj] == 1)
                        {
                            running_time += r.add_stop;
                        }
                        link_station(now_station, next_station, now_state_range[j], next_state_range[jj], running_time);
                    }
                }
            }//连接所有站间
            for (int i = 1; i < r.station_num - 1; i++)
            {
                int max_wait_time = int.Parse((string)r.max_waiting_time.Rows[train_num - 1][i]);
                int min_wait_time = int.Parse((string)r.min_waiting_time.Rows[train_num - 1][i]);
                if (min_wait_time == 0)
                {
                    min_wait_time = 1;
                }
                for (int t = 1; t <= time_len; t++)
                {
                    int now_node = station_time2node_num(i + 1, 1, t);
                    for (int ii = min_wait_time; ii <= max_wait_time; ii++)
                    {
                        if (t+ii>time_len)
                        {
                            break;
                        }
                        int next_node = station_time2node_num(i + 1, 2, t + ii);
                        s_t_mat[now_node - 1, next_node - 1] = ii;
                    }
                }
            }//连接所有站内
        }        
        public void remove_arc(int[] path, bool same_dir)
        {
            List<int> forbid_node_list = forbid_node(path, same_dir);
            foreach (var node in forbid_node_list)
            {
                for (int i = 0; i < s_t_mat.GetLength(0); i++)
                {
                    if (i != node - 1)
                    {
                        s_t_mat[node - 1, i] = int.MaxValue;
                        s_t_mat[i, node - 1] = int.MaxValue;
                    }
                }
            }
            forbid_cross(path,same_dir);

        }
        public void remove_arc(int[] path)
        {
            List<int> forbid_node_list = forbid_node(path);
            foreach (var node in forbid_node_list)
            {
                for (int i = 0; i < s_t_mat.GetLength(0); i++)
                {
                    if (i != node - 1)
                    {
                        s_t_mat[node - 1, i] = int.MaxValue;
                    }
                }
            }
            forbid_cross(path,true);
        }
        public void generate_nexta(int[] path,int train_id,DataTable node,DataTable road_link,DataTable agent,ref int road_link_id)
        {
            DataRow now_agent = agent.NewRow();
            now_agent[0] = train_id;
            string time_per = null;
            int path_cost = 0;
            string node_seq = null;
            string time_seq = null;
            for (int i = 0; i < path.Length; i++)
            {
                int[] sst = node_num2station_state_time(path[i]);
                int node_row_num = (sst[0] - 1) * time_len + sst[2];
                int zone_id = get_zone_id(sst[0], sst[2]);
                if (i==0)
                {
                    now_agent[1] = zone_id;
                    now_agent[3] = sst[0] * 1000000 + time_add2(r.start_time, sst[2]-1);
                    time_per += time_int2string(time_add2(r.start_time, sst[2]-1)) +"_";
                    path_cost = sst[2];                    
                }
                if (i== path.Length-1)
                {
                    now_agent[2] = zone_id;
                    now_agent[4] = sst[0] * 1000000 + time_add2(r.start_time, sst[2]);
                    time_per += time_int2string(time_add2(r.start_time, sst[2]-1));
                    path_cost = sst[2] - path_cost;
                }
                time_seq += time_int2string(time_add2(r.start_time, sst[2]-1)) +";";
                node_seq += (sst[0] * 1000000 + time_add2(r.start_time, sst[2]-1)).ToString()+";";
                node.Rows[node_row_num - 1][3] = zone_id;
                node.Rows[node_row_num - 1][4] = 1;
            }
            now_agent[5] = r.trian_type[train_num];
            now_agent[6] = time_per;
            now_agent[7] = 1;
            for (int i = 8; i <= 10; i++)
            {
                now_agent[i] = path_cost;
            }
            now_agent[11] = node_seq;
            now_agent[12] = time_seq;
            agent.Rows.Add(now_agent);
            for (int i = 0; i < path.Length-1; i++)
            {
                int[] now_node= node_num2station_state_time(path[i]);
                int[] next_node = node_num2station_state_time(path[i+1]);
                DataRow dr = road_link.NewRow();
                dr[1] = road_link_id++;
                int from_node_id = (now_node[0] * 1000000) + time_add2(r.start_time, now_node[2]-1);
                int to_node_id = (next_node[0] * 1000000) + time_add2(r.start_time, next_node[2]-1);
                int cost = next_node[2] - now_node[2];
                dr[2] = from_node_id;
                dr[3] = to_node_id;
                dr[5] = 1;
                dr[6] = cost;
                for (int ii = 7; ii <= 10; ii++)
                {
                    dr[ii] = 1;
                }
                dr[11] = cost;
                road_link.Rows.Add(dr);
            }

        }

        //工具函数
        public int[] Dijkstra(int 起点, int 终点)
        {
            int i = 起点;
            int 列数 = s_t_mat.GetLength(1);
            List<int> list = new List<int>();
            for (int ii = 1; ii <= 列数; ii++)
            {
                list.Add(ii);
            }
            int[] pred = new int[列数];
            for (int ii = 0; ii < 列数; ii++)
            {
                pred[ii] = 起点;
            }
            double[] d = new double[列数];
            for (int ii = 0; ii < 列数; ii++)
            {
                d[ii] = double.PositiveInfinity;
            };
            d[起点 - 1] = 0;
            pred[ 起点 - 1] = 起点;
            list.RemoveAt(i - 1);
            while (list.Count != 0)
            {
                for (int k = 1; k <= list.Count; k++)
                {
                    int j = list[k - 1];
                    if (d[j - 1] > d[i - 1] + s_t_mat[i - 1, j - 1] && s_t_mat[i - 1, j - 1]!=int.MaxValue)
                    {
                        d [j - 1] = d[ i - 1] + s_t_mat[i - 1, j - 1];
                        pred [j - 1] = i;
                    }
                }
                double[] d_temp = new double[list.Count];
                for (int ii = 1; ii <= list.Count; ii++)
                {
                    d_temp[ii - 1] = d[list[ii - 1] - 1];
                }
                int index = 0;
                for (int ii = 0; ii < d_temp.Length; ii++)
                {
                    if (d_temp[index] > d_temp[ii])
                    {
                        index = ii;
                    }
                }
                i = list[index];
                list.RemoveAt(index);

            }
            int[] path = new int[1] { 0 };
            if (d[终点 - 1] != double.PositiveInfinity)
            {
                path[0] = 终点;
                int now_node = 终点;
                while (now_node != 起点)
                {
                    double pre_node = pred[now_node - 1];
                    int[] aaa = new int[1] { (int)pre_node };
                    path = 合并数组(aaa, path);
                    now_node = (int)pre_node;
                }
            }
            return path;
        }
        public static int[] 合并数组(int[] a1, int[] a2)
        {
            int l_a1 = a1.Length;
            int l_a2 = a2.Length;
            int[] a3 = new int[l_a1 + l_a2];
            for (int i = 1; i <= l_a1 + l_a2; i++)
            {
                if (i <= l_a1)
                {
                    a3[i - 1] = a1[i - 1];
                }
                else
                {
                    a3[i - 1] = a2[i - l_a1 - 1];
                }
            }
            return a3;
        }
        private int station_time2node_num(int station, int state, int time)
        {
            if (station == 1)
            {
                return 2 + time;
            }
            else if (station == r.station_num)
            {
                return 2 + (station - 2) * 3 * time_len + time_len + time;
            }
            else
            {
                return 2 + (station - 2) * 3 * time_len + time_len + (state - 1) * time_len + time;
            }
        }
        private int[] node_num2station_state_time(int node_num)
        {
            int station, state, time;
            int num = (node_num - 2) / time_len;
            time = (node_num - 2) % time_len;
            if (time == 0)
            {
                time = time_len;
            }
            if (num >= 1)
            {
                station = (num - 1) / 3 + 2;
                state = num - 1 - (station - 2) * 3 + 1;
            }
            else
            {
                station = 1;
                if (station_range[0]==1)
                {
                    state = 2;
                }
                else
                {
                    state = 1;
                }
            }
            if (station == r.station_num)
            {
                if (station_range[0] == 1)
                {
                    state = 1;
                }
                else
                {
                    state = 2;
                }
            }
            return new int[3] { station, state, time };

        }
        private int time_sub(int t1, int t2)
        {
            int h1 = t1 / 100;
            int m1 = t1 % 100;
            int h2 = t2 / 100;
            int m2 = t2 % 100;
            if (m1 - m2 >= 0)
            {
                return (h1 - h2) * 100 + m1 - m2;
            }
            else
            {
                return (h1 - h2 - 1) * 100 + m1 - m2 + 60;
            }
        }
        private int time_sub2min(int t1, int t2)
        {
            int h1 = t1 / 100;
            int m1 = t1 % 100;
            int h2 = t2 / 100;
            int m2 = t2 % 100;
            return (h1 - h2) * 60 + m1 - m2;
        }
        private int time_add(int t1, int t2)
        {
            int h1 = t1 / 100;
            int m1 = t1 % 100;
            int h2 = t2 / 100;
            int m2 = t2 % 100;
            if (m1 + m2 < 60)
            {
                return (h1 + h2) * 100 + m1 + m2;
            }
            else
            {
                return (h1 + h2 + 1) * 100 + m1 + m2 - 60;
            }
        }
        private int time_add2(int t1, int t2)
        {
            int h1 = t1 / 100;
            int m1 = t1 % 100;
            if (m1+t2<60)
            {
                return (h1) * 100 + (m1 + t2);
            }
            else
            {
                int add_h = (m1 + t2) / 60;
                int add_m = (m1 + t2) % 60;
                return (h1 + add_h) * 100 + add_m;
            }
        }
        private void link_station(int now_station, int next_station, int now_state, int next_state, int running_time)
        {            
            for (int t = 1; t <= time_len; t++)
            {
                if (t + running_time >= time_len)
                {
                    break;
                }
                int now_node = station_time2node_num(now_station, now_state, t);
                int next_node = station_time2node_num(next_station, next_state, t + running_time);
                s_t_mat[now_node - 1, next_node - 1] = running_time;
            }
        }
        private void forbid_cross(int[] path,bool same_dir)
        {
            for (int i = 2; i <= path.Length - 1; i++)
            {
                int[] now_sst = node_num2station_state_time(path[i - 2]);
                int[] next_sst = node_num2station_state_time(path[i-1]);
                List<int> f_state = new List<int>(); List<int> p_state = new List<int>();
                if (same_dir)
                {
                    //同向
                    if (now_sst[0] != station_range[0])
                    {
                        f_state.Add(2); f_state.Add(3);
                    }
                    else
                    {
                        f_state.Add(2);
                    }
                    if (next_sst[0] != station_range[r.station_num - 1])
                    {
                        p_state.Add(1); p_state.Add(3);
                    }
                    else
                    {
                        p_state.Add(1);
                    }
                    for (int f_time = 1; f_time <= now_sst[2]; f_time++)
                    {
                        for (int p_time = next_sst[2]; p_time <= time_len; p_time++)
                        {
                            foreach (var fs in f_state)
                            {
                                int f_node = station_time2node_num(now_sst[0], fs, f_time);
                                foreach (var ps in p_state)
                                {
                                    int p_node = station_time2node_num(next_sst[0], ps, p_time);
                                    s_t_mat[f_node - 1, p_node - 1] = int.MaxValue;
                                }
                            }
                        }
                    }
                }
                else
                {
                    //反向
                    f_state = new List<int>(); p_state = new List<int>();
                    if (now_sst[0] != station_range[r.station_num - 1])
                    {
                        p_state.Add(1); p_state.Add(3);
                    }
                    else
                    {
                        p_state.Add(1);
                    }
                    if (next_sst[0] != station_range[0])
                    {
                        f_state.Add(2); f_state.Add(3);
                    }
                    else
                    {
                        f_state.Add(2);
                    }
                    for (int f_time = 1; f_time <= next_sst[2]; f_time++)
                    {
                        for (int p_time = now_sst[2]; p_time <= time_len; p_time++)
                        {
                            foreach (var fs in f_state)
                            {
                                int f_node = station_time2node_num(next_sst[0], fs, f_time);
                                foreach (var ps in p_state)
                                {
                                    int p_node = station_time2node_num(now_sst[0], ps, p_time);
                                    s_t_mat[f_node - 1, p_node - 1] = int.MaxValue;                                   
                                }                                
                            }
                        }
                    }
                }
            }
        }
        private List<int> get_forbid_node_list(int now_station, int now_state, int now_time, int last_running_time,int next_running_time ,bool same_dir)
        {
            int T_bu = int.Parse((string)r.interval.Rows[now_station - 1][1]);
            int T_hui = int.Parse((string)r.interval.Rows[now_station - 1][2]);
            int T_lian = int.Parse((string)r.interval.Rows[now_station - 1][3]);
            int T_daofa = int.Parse((string)r.interval.Rows[now_station - 1][4]);
            int T_fadao = int.Parse((string)r.interval.Rows[now_station - 1][5]);
            List<int> forbid_node_list = new List<int>();
            int last_station, next_station;
            if (station_range[0] == 1)
            {
                last_station = now_station - 1 >= 1 ? now_station - 1 : 0;
                next_station = now_station + 1 <= r.station_num ? now_station + 1 : 0;
            }
            else
            {
                last_station = now_station + 1 <= r.station_num ? now_station + 1 : 0;
                next_station = now_station - 1 >= 1 ? now_station - 1 : 0;
            }
            if (now_state == 2)
            {
                if (same_dir)
                {
                    if (now_station != station_range[0])
                    {
                        for (int time = now_time; time <= (now_time + T_fadao - 1 < time_len ? now_time + T_fadao - 1 : time_len); time++)//不同时发到间隔
                        {
                            forbid_node_list.Add(station_time2node_num(now_station, 1, time));
                        }
                        for (int time = now_time - T_daofa + 1 >= 1 ? now_time - T_daofa + 1 : 1; time <= now_time; time++)//不同时到发间隔
                        {
                            forbid_node_list.Add(station_time2node_num(now_station, 1, time));
                        }
                    }
                    if (next_station != 0)
                    {
                        if (next_running_time!=0)
                        {
                            for (int time = (now_time - T_lian +1 > 1 ? now_time - T_lian +1 : 1); time <= (now_time + next_running_time <= time_len ? now_time + next_running_time : time_len); time++)//连发间隔时间
                            {
                                forbid_node_list.Add(station_time2node_num(next_station, 1, time));
                                if (next_station != station_range[r.station_num - 1])
                                {
                                    forbid_node_list.Add(station_time2node_num(next_station, 3, time));
                                }
                            }
                        }                        
                    }
                }
                else
                {
                    for (int time = now_time - T_hui + 1 >= 1 ? now_time - T_hui + 1 : 1; time <= now_time; time++)//会车间隔时间
                    {
                        if (now_station != station_range[r.station_num-1])
                        {
                            forbid_node_list.Add(station_time2node_num(now_station, 1, time));
                            forbid_node_list.Add(station_time2node_num(now_station, 3, time));
                        }
                        else
                        {
                            forbid_node_list.Add(station_time2node_num(now_station, 1, time));
                        }
                    }
                }
            }
            else if (now_state == 1)
            {
                if (same_dir)
                {
                    if (last_station != 0)
                    {
                        if (last_running_time!=0)
                        {
                            for (int time = (now_time - last_running_time >= 1 ? now_time - last_running_time : 1); time <= (now_time + T_lian - 1 <= time_len ? now_time + T_lian - 1 : time_len); time++)//连发间隔时间
                            {
                                forbid_node_list.Add(station_time2node_num(last_station, 2, time));
                                if (last_station != station_range[0])
                                {
                                    forbid_node_list.Add(station_time2node_num(last_station, 3, time));
                                }
                            }
                        }
                    }
                    if (now_station != station_range[r.station_num - 1])
                    {
                        for (int time = now_time; time <= (now_time + T_daofa - 1 <= time_len ? now_time + T_daofa - 1 : time_len); time++)//不同时到发
                        {
                            forbid_node_list.Add(station_time2node_num(now_station, 2, time));
                        }
                        for (int time = (now_time - T_fadao + 1 > 1 ? now_time - T_fadao + 1 : 1); time <= now_time; time++)//不同时发到
                        {
                            forbid_node_list.Add(station_time2node_num(now_station, 2, time));
                        }
                    }
                }
                else
                {
                    for (int time = (now_time - T_bu+1 > 1 ? now_time - T_bu+1 : 1); time <= (now_time + T_bu - 1 < time_len ? now_time + T_bu - 1 : time_len); time++)//不同时到达
                    {
                        if (now_station != station_range[r.station_num - 1])
                        {
                            forbid_node_list.Add(station_time2node_num(now_station, 1, time));
                            forbid_node_list.Add(station_time2node_num(now_station, 3, time));
                        }
                    }
                    for (int time = now_time; time <= (now_time + T_hui - 1 < time_len ? now_time + T_hui - 1 : time_len); time++)//会车间隔
                    {                        
                        forbid_node_list.Add(station_time2node_num(now_station, 2, time));  
                    }
                }
            }
            else if (now_state == 3)
            {
                if (same_dir)
                {
                    if (last_station != 0)
                    {
                        if (last_running_time!=0)
                        {
                            for (int time = (now_time - last_running_time > 1 ? now_time - last_running_time : 1); time <= (now_time + T_lian - 1 < time_len ? now_time + T_lian - 1 : time_len); time++)//连发间隔时间
                            {
                                forbid_node_list.Add(station_time2node_num(last_station, 2, time));
                                if (last_station != station_range[0])
                                {
                                    forbid_node_list.Add(station_time2node_num(last_station, 3, time));
                                }
                            }
                        }
                    }
                    if (next_station!=0)
                    {
                        if (next_running_time!=0)
                        {
                            for (int time = (now_time - T_lian +1 > 1 ? now_time - T_lian +1 : 1); time <= (now_time + next_running_time <= time_len ? now_time + next_running_time : time_len); time++)//连发间隔时间
                            {
                                forbid_node_list.Add(station_time2node_num(next_station, 1, time));
                                if (next_station != station_range[r.station_num - 1])
                                {
                                    forbid_node_list.Add(station_time2node_num(next_station, 3, time));
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int time = now_time; time <= (now_time + T_hui - 1 < time_len ? now_time + T_hui - 1 : time_len); time++)//会车间隔
                    {
                        forbid_node_list.Add(station_time2node_num(now_station, 2, time));
                    }
                    for (int time = (now_time - T_bu + 1 > 1 ? now_time - T_bu + 1 : 1); time <= now_time; time++)//不同时到达
                    {
                        forbid_node_list.Add(station_time2node_num(now_station, 1, time));
                    }
                }
            }
            return forbid_node_list;
        }
        private List<int> get_forbid_node_list(int now_station, int now_state, int now_time)
        {
            List<int> forbid_node_list = new List<int>();
            int time_interval;
            if (now_state==1)
            {
                time_interval = int.Parse((string)r.interval.Rows[now_station - 1][1]);
            }
            else if (now_state == 2)
            {
                time_interval = int.Parse((string)r.interval.Rows[now_station - 1][2]);
            }
            else
            {
                time_interval = int.Parse((string)r.interval.Rows[now_station - 1][3]);
            }
            for (int i = (now_time-time_interval>=1?now_time-time_interval:1); i < (now_time+time_interval<=time_len?now_time+time_interval:time_len); i++)
            {
                if (now_state==1 || now_state==3)
                {
                    forbid_node_list.Add(station_time2node_num(now_station, 1, i));
                    if (now_station != station_range[r.station_num-1])
                    {
                        forbid_node_list.Add(station_time2node_num(now_station, 3, i));
                    }
                }
                else
                {
                    forbid_node_list.Add(station_time2node_num(now_station, 2, i));
                    if (now_station != 1)
                    {
                        forbid_node_list.Add(station_time2node_num(now_station, 3, i));
                    }
                }
            }
            return forbid_node_list;
        }
        private List<int> forbid_node(int[] path, bool same_dir)
        {
            List<int> forbid_node_list = new List<int>();
            for (int i = 1; i < path.Length - 1; i++)
            {
                int[] sst = node_num2station_state_time(path[i]);
                if (!same_dir && (sst[0]==1 || sst[0]==r.station_num) )
                {
                    if (sst[1] == 1)
                        sst[1] = 2;
                    else if (sst[1] == 2)
                        sst[1] = 1;

                }
                int last_running_time = 0, next_running_time=0;
                if (i >= 2 &&sst[1]!=2)
                {
                    int[] last_sst = node_num2station_state_time(path[i - 1]);
                    last_running_time = sst[2] - last_sst[2];
                }
                if (i<= path.Length - 2 &&sst[1]!=1)
                {
                    int[] next_sst = node_num2station_state_time(path[i + 1]);
                    next_running_time = next_sst[2] - sst[2];
                }
                forbid_node_list.AddRange(get_forbid_node_list(sst[0], sst[1], sst[2], last_running_time,next_running_time, same_dir));   
            }
            return forbid_node_list;
        }
        private List<int> forbid_node(int[] path)
        {
            List<int> forbid_node_list = new List<int>();
            for (int i = 1; i < path.Length - 1; i++)
            {
                int[] sst = node_num2station_state_time(path[i]);
                forbid_node_list.AddRange(get_forbid_node_list(sst[0], sst[1], sst[2]));
            }
            return forbid_node_list;
        }
        public static DataTable define_agent()
        {
            DataTable agent = new DataTable();
            DataColumn agent_id = new DataColumn("agent_id", typeof(int));
            DataColumn o_zone_id = new DataColumn("o_zone_id", typeof(long));
            DataColumn d_zone_id = new DataColumn("d_zone_id", typeof(long));
            DataColumn o_node_id = new DataColumn("o_node_id", typeof(long));
            DataColumn d_node_id = new DataColumn("d_node_id", typeof(long));
            DataColumn agent_type = new DataColumn("agent_type", typeof(string));
            DataColumn time_period = new DataColumn("time_period", typeof(string));
            DataColumn volume = new DataColumn("volume", typeof(int));
            DataColumn cost = new DataColumn("cost", typeof(int));
            DataColumn travel_time = new DataColumn("travel_time", typeof(int));
            DataColumn distance = new DataColumn("distance", typeof(int));
            DataColumn node_sequence = new DataColumn("node_sequence", typeof(string));
            DataColumn time_sequence = new DataColumn("time_sequence", typeof(string));
            agent.Columns.AddRange(new DataColumn[13] { agent_id, o_zone_id, d_zone_id, o_node_id, d_node_id, agent_type, time_period, volume, cost, travel_time, distance, node_sequence, time_sequence });
            return agent;
        }
        public static DataTable define_road_link()
        {
            DataTable road_link = new DataTable();
            DataColumn name = new DataColumn("name", typeof(string));
            DataColumn road_link_id = new DataColumn("road_link_id", typeof(int));
            DataColumn from_node_id = new DataColumn("from_node_id", typeof(long));
            DataColumn to_node_id = new DataColumn("to_node_id", typeof(long));
            DataColumn facility_type = new DataColumn("facility_type", typeof(int));
            DataColumn dir_flag = new DataColumn("dir_flag", typeof(int));
            DataColumn length = new DataColumn("length", typeof(int));
            DataColumn lanes = new DataColumn("lanes", typeof(int));
            DataColumn capacity = new DataColumn("capacity", typeof(int));
            DataColumn free_speed = new DataColumn("free_speed", typeof(int));
            DataColumn link_type = new DataColumn("link_type", typeof(int));
            DataColumn cost = new DataColumn("cost", typeof(int));
            road_link.Columns.AddRange(new DataColumn[12] { name, road_link_id, from_node_id, to_node_id, facility_type, dir_flag, length, lanes, capacity, free_speed, link_type, cost });
            return road_link;
        }
        public static DataTable define_node()
        {
            DataTable node = new DataTable();
            DataColumn name = new DataColumn("name", typeof(string));
            DataColumn phy_node_id = new DataColumn("physical_node_id", typeof(int));
            DataColumn node_id = new DataColumn("node_id", typeof(long));
            DataColumn zone_id = new DataColumn("zone_id", typeof(long));
            DataColumn node_type = new DataColumn("node_type", typeof(int));
            DataColumn control_type = new DataColumn("control_type", typeof(int));
            DataColumn x_coord = new DataColumn("x_coord", typeof(int));
            DataColumn y_coord = new DataColumn("y_coord", typeof(int));
            node.Columns.AddRange(new DataColumn[8] { name, phy_node_id, node_id, zone_id, node_type, control_type, x_coord, y_coord });
            return node;
        }        
        public DataTable init_node(DataTable node)
        {            
            for (int i = 1; i <= r.station_num; i++)
            {
                for (int t = 1; t <= time_len; t++)
                {
                    DataRow dr = node.NewRow();
                    dr[1] = i;
                    dr[2] = i*1000000 + time_add2(r.start_time, t - 1);
                    dr[3] = 0;
                    dr[4] = 0;
                    dr[6] = t * 100;
                    dr[7] = i * 1000;
                    node.Rows.Add(dr);
                }
            }
            return node;
        }
        private int get_zone_id(int now_station, int now_time)
        {
            if (now_station != 1 && now_station != r.station_num)
            {
                return 0;
            }
            now_time = time_add2(r.start_time, now_time);
            for (int i = 0; i < r.zone.Rows.Count; i++)                 
            {
                int check_station = int.Parse((string)r.zone.Rows[i][3]);
                if (now_time >= int.Parse((string)r.zone.Rows[i][1]) && now_time <= int.Parse((string)r.zone.Rows[i][2]) && now_station == check_station)
                {
                    return i+1;
                }
            }
            return 0;
        }
        private string time_int2string(int t)
        {
            if (t < 1000)
            {
                return "0" + t.ToString();
            }
            else
            {
                return t.ToString();
            }
        }
    }
}
