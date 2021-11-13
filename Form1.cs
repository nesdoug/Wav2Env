using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Wav2Env
{
    public partial class Form1 : Form
    {
        // fuzzy math for maximum sizes
        // expected sample rate 44100 or 48000
        // expected file size max = 60/50 = 1.1-1.3 seconds
        // 48000 * 1.3 * 2 channels * 3 bytes = 374400
        public const int DATA_SIZE_MAX = 395000; // a little extra to
        public const int DATA_SIZE_MAX2 = 385000; // fix bugs
        // 48000 * 1.3 = 62300
        public const int DATA_SIZE_SM = 62500;
        public const int FINAL_SIZE = 64;
        public static byte[] Wav_Array = new byte[DATA_SIZE_MAX];
        public static byte[] Data_Array = new byte[DATA_SIZE_SM];
        public static int[] Final_Array = new int[FINAL_SIZE];

        public static int file_size, data_size, data_size_min;
        public static int data_size_sm, sample_size, step_size;
        public static int high_byte_offset, high_byte_offset2;
        public static int sample_rate, num_samples, chunk_size, num_chunks;
        public static int chunk_size_q; // 1/4
        // chunk is a group of samples, size determined by Sample Rate
        public static bool is_stereo = false;
        public static int data_start = 44;
        public static string out_string1 = "";
        public static string filename = "";
        public static int biggest;
        public static bool has_loaded = false;
        public static bool do_one_more = false;

        //512 83
        public static Bitmap image_map = new Bitmap(512, 80);

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0; // NTSC
            comboBox2.SelectedIndex = 1; // 15
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(has_loaded == true)
            {
                // redo the calculation
                Do_All_Calculus();

                string more_text = filename;
                more_text += "  -  Size = ";
                more_text += num_chunks.ToString();

                label2.Text = more_text;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (has_loaded == true)
            {
                // redo the calculation
                Do_All_Calculus();

                string more_text = filename;
                more_text += "  -  Size = ";
                more_text += num_chunks.ToString();

                label2.Text = more_text;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("version 1.0, by Doug Fraker, 2021.\n\nnesdoug.com");
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // load a WAV file

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Open a WAV file";
            openFileDialog1.Filter = "WAV File (*.wav)|*.wav|All files (*.*)|*.*";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {

                System.IO.FileStream fs = (System.IO.FileStream)openFileDialog1.OpenFile();

                filename = Path.GetFileName(fs.Name);

                if (fs.Length > 0)
                {
                    // clear the data array
                    for(int i = 0; i < DATA_SIZE_SM; i++)
                    {
                        Data_Array[i] = 0;
                    }
                    for(int i = 0; i < DATA_SIZE_MAX; i++)
                    {
                        Wav_Array[i] = 0;
                    }

                    
                    if(fs.Length > DATA_SIZE_MAX)
                    {
                        file_size = DATA_SIZE_MAX;
                    }
                    else
                    {
                        file_size = (int)fs.Length;
                    }
                    for (int i = 0; i < file_size; i++)
                    {
                        Wav_Array[i] = (byte)fs.ReadByte();
                    }

                    fs.Close();

                    // check to see if wav file
                    if((Wav_Array[0] == 0x52) && // R
                       (Wav_Array[1] == 0x49) && // I
                       (Wav_Array[2] == 0x46) && // F
                       (Wav_Array[3] == 0x46) && // F
                       (Wav_Array[8] == 0x57) && // W
                       (Wav_Array[9] == 0x41) && // A
                       (Wav_Array[10] == 0x56) && // V
                       (Wav_Array[11] == 0x45) && // E
                       (Wav_Array[20] == 1)) // PCM mode
                    {
                        
                        sample_size = Wav_Array[34] / 8;
                        if((sample_size < 1) || (sample_size > 3))
                        {
                            MessageBox.Show("Error. Unexpected bits per sample.");
                            return;
                        }

                        if (Wav_Array[22] == 1) // # of channels
                        {
                            is_stereo = false; // mono
                            step_size = sample_size; // 1,2,3
                            data_size_min = step_size;
                            high_byte_offset = step_size - 1;
                        }
                        else if (Wav_Array[22] == 2) // stereo
                        {
                            is_stereo = true;
                            data_size_min = 4;
                            step_size = sample_size * 2; // 2,4,6
                            data_size_min = step_size;
                            high_byte_offset = sample_size - 1; // L
                            high_byte_offset2 = step_size - 1; // R
                        }
                        else
                        {
                            MessageBox.Show("Error. Unexpected value. Mono vs Stereo.");
                            return;
                        }
                        byte b1 = Wav_Array[24]; // sample rate
                        byte b2 = Wav_Array[25];
                        byte b3 = Wav_Array[26];
                        byte b4 = Wav_Array[27];
                        sample_rate = b1 + (b2 << 8) + (b3 << 16) + (b4 << 24);
                        if(sample_rate > 48000)
                        {
                            MessageBox.Show("Error. Unexpected sample rate > 48000");
                            return;
                        }
                        if(sample_rate < 8000)
                        {
                            MessageBox.Show("Error. Unexpected sample rate < 8000");
                            return;
                        }
                        
                        Find_Data_Start();

                        b1 = Wav_Array[data_start - 4]; // data size
                        b2 = Wav_Array[data_start - 3];
                        b3 = Wav_Array[data_start - 2];
                        b4 = Wav_Array[data_start - 1];
                        int temp_data_size = b1 + (b2 << 8) + (b3 << 16) + (b4 << 24);
                        if(temp_data_size < data_size_min)
                        {
                            MessageBox.Show("Error. Unexpected low data size.");
                            return;
                        }
                        if(temp_data_size < DATA_SIZE_MAX2)
                        {
                            data_size = temp_data_size;
                        }
                        else // >= DATA_SIZE_MAX
                        {
                            data_size = DATA_SIZE_MAX2;
                        }

                        Convert_Abs();

                        Do_All_Calculus();

                        // change the label
                        
                        string more_text = filename;
                        more_text += "  -  Size = ";
                        more_text += num_chunks.ToString();

                        label2.Text = more_text;

                        has_loaded = true;

                    }
                    else // not a wav file
                    {
                        MessageBox.Show("File error. Should be 16 bit PCM WAV File.");
                        return;
                    }

                }
                else
                {
                    MessageBox.Show("File size error.");
                }

                fs.Close();

            }

        }



        public void Do_All_Calculus()
        {
            // find max value, use for normalization
            Find_Biggest();
            

            // get chunk size and number
            double temp_db = 0.0;
            
            if (comboBox1.SelectedIndex == 0)
            {
                chunk_size = sample_rate / 60;
            }
            else
            {
                chunk_size = sample_rate / 50;
            }
            chunk_size_q = (chunk_size + 2) / 4;
            num_chunks = data_size_sm / chunk_size; // rounds down
            if (num_chunks > FINAL_SIZE) num_chunks = FINAL_SIZE;

            // should we do an extra half chunk ?
            int total_processed = num_chunks * chunk_size;
            int half_chunk = (chunk_size + 1)/ 2;
            do_one_more = false;
            if (total_processed + half_chunk < data_size_sm)
            {
                do_one_more = true;
            }


            if (num_chunks > 0)
            {
                Process_Chunks();
                // final array size = num_chunks
            }
            else // less than 1 chunk
            {
                num_chunks = 1;
                Process_One_Chunk();
            }

            // recalculate biggest again
            biggest = 0;
            for (int i = 0; i < num_chunks; i++)
            {
                if (Final_Array[i] > biggest)
                {
                    biggest = Final_Array[i];
                }
            } // biggest now 0-128
            if (biggest == 0) // prevent divide by zero
            {
                biggest = 128; // any value != zero
            }



            // normalize

            double multiplier = 0.0;
            double normal_val = 0.0;

            switch (comboBox2.SelectedIndex)
            {
                case 0:
                    normal_val = 16.0;
                    break;
                case 1:
                default:
                    normal_val = 15.0;
                    break;
                case 2:
                    normal_val = 14.0;
                    break;
                case 3:
                    normal_val = 13.0;
                    break;
                case 4:
                    normal_val = 12.0;
                    break;
                case 5:
                    normal_val = 11.0;
                    break;
                case 6:
                    normal_val = 10.0;
                    break;
                case 7:
                    normal_val = 9.0;
                    break;
                case 8:
                    normal_val = 8.0;
                    break;
                case 9:
                    normal_val = 7.0;
                    break;
                case 10:
                    normal_val = 6.0;
                    break;
                case 11:
                    normal_val = 5.0;
                    break;
                case 12:
                    normal_val = 4.0;
                    break;
                case 13:
                    normal_val = 3.0;
                    break;
                case 14:
                    normal_val = 2.0;
                    break;
                case 15:
                    normal_val = 0.0; // don't normalize
                    break;
            }

            if (normal_val == 0.0) // don't normalize
            {
                multiplier = 15.0 / 128.0;
            }
            else // normalize
            {
                multiplier = 15.0 / biggest;
                multiplier = multiplier * normal_val / 15.0;
            }

            for (int i = 0; i < num_chunks; i++)
            {
                temp_db = Final_Array[i] * multiplier;
                temp_db += 0.5; // for better rouding down
                if (temp_db > 15.0) temp_db = 15.0;
                if (temp_db < 0) temp_db = 0;
                Final_Array[i] = (int)temp_db;
            }


            // trim trailing zeros

            while(num_chunks > 1)
            {
                if((Final_Array[num_chunks-1] == 0) && (Final_Array[num_chunks-2] == 0))
                {
                    num_chunks--;
                }
                else
                {
                    break;
                }
            }


            // convert to string -> textboxes

            out_string1 = "";
            
            for (int i = 0; i < num_chunks; i++)
            {
                out_string1 += Final_Array[i].ToString();
                out_string1 += " ";
                
            }
            textBox1.Text = out_string1;
            


            // convert to graphics -> bitmap -> picturebox

            // blank it
            for(int y = 0; y < 80; y++)
            {
                for(int x = 0; x < 512; x++)
                {
                    image_map.SetPixel(x, y, Color.DarkGray);
                }
            }

            for (int i = 0; i < num_chunks; i++)
            {
                
                int start_y = 80 - (Final_Array[i] * 5);
                if (Final_Array[i] == 0)
                {
                    start_y = 79;
                }
                for (int x = 0; x < 7; x++)
                {
                    int x_offset = (i * 8) + x;
                    
                    for(int y = start_y; y < 80; y++)
                    {
                        image_map.SetPixel(x_offset, y, Color.Black);
                    }
                }
            }

            pictureBox1.Image = image_map;
        }



        public void Find_Data_Start()
        {
            for(int i = 36; i < file_size-3; i++)
            {
                if((Wav_Array[i] == 0x64) && // d
                   (Wav_Array[i+1] == 0x61) && // a
                   (Wav_Array[i+2] == 0x74) && // t
                   (Wav_Array[i+3] == 0x61) ) // a
                {
                    data_start = i + 8;
                    return;
                }
            }
            data_start = 44; // if all fails, use standard
        }


        public void Convert_Abs()
        {
            // convert a 16-24 bit signed to 8 bit unsigned 0-128

            int offset2 = 0;
            
            if (is_stereo == false) // mono = 2 bytes per sample
            {
                for (int i = 0; i < data_size; i += step_size)
                {
                    int j = i + data_start; // skip the header
                    
                    byte b2 = Wav_Array[j + high_byte_offset]; // high

                    byte a_value;
                    if(b2 < 128) // negative
                    {
                        a_value = b2;
                    }
                    else
                    {
                        a_value = (byte)(256 - b2);
                    }

                    Data_Array[offset2] = a_value;
                    offset2++;
                    if (offset2 >= DATA_SIZE_SM) break;
                }
            }
            else // stereo = 4 bytes per sample, merge L and R
            {
                for (int i = 0; i < data_size; i += step_size)
                {
                    int j = i + data_start; // skip the header

                    byte b2 = Wav_Array[j + high_byte_offset]; // L
                    byte b4 = Wav_Array[j + high_byte_offset2]; // R

                    byte a_valueL;
                    if (b2 < 128) // negative
                    {
                        a_valueL = b2;
                    }
                    else
                    {
                        a_valueL = (byte)(256 - b2);
                    }
                    byte a_valueR;
                    if (b4 < 128) // negative
                    {
                        a_valueR = b4;
                    }
                    else
                    {
                        a_valueR = (byte)(256 - b4);
                    }

                    int average = ((int)a_valueL + (int)a_valueR) / 2;
                    Data_Array[offset2] = (byte)average;
                    offset2++;
                    if (offset2 >= DATA_SIZE_SM) break;
                }
            }
            data_size_sm = offset2; // size of the smaller array
        }


        public void Find_Biggest()
        {
            biggest = 0;
            for(int i = 0; i < data_size_sm; i++)
            {
                if(Data_Array[i] > biggest)
                {
                    biggest = Data_Array[i];
                }
            }
            //error check, shouldn't be more than 128
            if (biggest > 128) biggest = 128;
        }


        public void Process_Chunks()
        {
            
            int biggest_ave = 0;
            int offset = 0;
            int offset_f = 0;

            int biggest_q1;
            int biggest_q2;
            int biggest_q3;
            int biggest_q4;
            int chunk_q_start, chunk_q_end;
            
            for (int ch = 0; ch < num_chunks; ch++)
            {
                
                biggest_q1 = 0;
                biggest_q2 = 0;
                biggest_q3 = 0;
                biggest_q4 = 0;
                chunk_q_start = 0;
                chunk_q_end = chunk_size_q;
                //do each quarter of chunk separately
                for (int ch_q = chunk_q_start; ch_q < chunk_q_end; ch_q++)
                {
                    if(Data_Array[ch_q + offset] > biggest_q1)
                    {
                        biggest_q1 = Data_Array[ch_q + offset];
                    }
                }
                chunk_q_start += chunk_size_q;
                chunk_q_end += chunk_size_q;
                for (int ch_q = chunk_q_start; ch_q < chunk_q_end; ch_q++)
                {
                    if (Data_Array[ch_q + offset] > biggest_q2)
                    {
                        biggest_q2 = Data_Array[ch_q + offset];
                    }
                }
                chunk_q_start += chunk_size_q;
                chunk_q_end += chunk_size_q;
                for (int ch_q = chunk_q_start; ch_q < chunk_q_end; ch_q++)
                {
                    if (Data_Array[ch_q + offset] > biggest_q3)
                    {
                        biggest_q3 = Data_Array[ch_q + offset];
                    }
                }
                chunk_q_start += chunk_size_q;
                chunk_q_end += chunk_size_q;
                for (int ch_q = chunk_q_start; ch_q < chunk_q_end; ch_q++)
                {
                    if (Data_Array[ch_q + offset] > biggest_q4)
                    {
                        biggest_q4 = Data_Array[ch_q + offset];
                    }
                }
                biggest_ave = (biggest_q1 + biggest_q2 + biggest_q3 + biggest_q4 + 2) / 4;

                Final_Array[offset_f] = biggest_ave;
                offset_f++;
                offset += chunk_size;

                
            }

            if (do_one_more == false) return;
            if (offset_f >= FINAL_SIZE) return;
            // do another half chunk, it's safe
            biggest_q1 = 0;
            biggest_q2 = 0;
            chunk_q_start = 0;
            chunk_q_end = chunk_size_q;
            //do each quarter of chunk separately
            for (int ch_q = chunk_q_start; ch_q < chunk_q_end; ch_q++)
            {
                if (Data_Array[ch_q + offset] > biggest_q1)
                {
                    biggest_q1 = Data_Array[ch_q + offset];
                }
            }
            chunk_q_start += chunk_size_q;
            chunk_q_end += chunk_size_q;
            for (int ch_q = chunk_q_start; ch_q < chunk_q_end; ch_q++)
            {
                if (Data_Array[ch_q + offset] > biggest_q2)
                {
                    biggest_q2 = Data_Array[ch_q + offset];
                }
            }
            biggest_ave = (biggest_q1 + biggest_q2 + 1) / 2;
            Final_Array[offset_f] = biggest_ave;
            num_chunks++;
        }

        public void Process_One_Chunk()
        {
            // unusually small wav file
            
            // just use the largest value
            Final_Array[0] = biggest;

            num_chunks = 1;


        }

    }
}
