using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBFtoMysql
{
    public partial class ui_Main : Form
    {
        MySqlConnectionStringBuilder mySqlConnectionStringBuilder;

        MySqlConnection connection;

        OleDbConnection connectionDBF;

        private string _databaseName;

        private string _pathDbf;

        public ui_Main()
        {
            InitializeComponent();
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txtPathDBF.Text = openFileDialog.FileName;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txtPathDBF.Text))
            {
                MessageBox.Show(Resources.Shared.FileNotExists, Resources.Shared.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            _pathDbf = txtPathDBF.Text;

            _databaseName = txtDatabase.Text;

            if (string.IsNullOrEmpty(_databaseName))
                _databaseName = Path.GetFileNameWithoutExtension(_pathDbf);

            uint puerto;

            if (!uint.TryParse(txtPort.Text, out puerto))
            {
                MessageBox.Show(Resources.Shared.PortIsInvalid, Resources.Shared.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder()
            {
                Server = txtHost.Text,

                Port = puerto,

                UserID = txtUser.Text,

                Password = txtPassword.Text,
            };

            connection = new MySqlConnection(mySqlConnectionStringBuilder.GetConnectionString(true));

            connectionDBF = new OleDbConnection("Provider=VFPOLEDB.1;Data Source=" + _pathDbf + "");

            progressBar.Style = ProgressBarStyle.Marquee;

            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string query;

            MySqlCommand mysqlCommand;

            string tableName;

            tableName = Path.GetFileNameWithoutExtension(_pathDbf);

            connection.Open();

            connectionDBF.Open();

            query = "SHOW DATABASES";

            mysqlCommand = new MySqlCommand(query, connection);

            MySqlDataAdapter adapterDatabase = new MySqlDataAdapter(mysqlCommand);

            DataTable dtDtabases = new DataTable();

            adapterDatabase.Fill(dtDtabases);

            var existDatabase = dtDtabases.AsEnumerable().FirstOrDefault(a => a.Field<string>("Database").Equals(_databaseName));

            if (existDatabase == null)
            {
                mysqlCommand = connection.CreateCommand();

                mysqlCommand.CommandText = $"CREATE DATABASE {_databaseName}";

                mysqlCommand.ExecuteNonQuery();
            }

            connection.Close();

            mySqlConnectionStringBuilder.Database = _databaseName;

            connection = new MySqlConnection(mySqlConnectionStringBuilder.GetConnectionString(true));

            connection.Open();

            query = "SHOW TABLES";

            mysqlCommand = new MySqlCommand(query, connection);

            MySqlDataAdapter adapterTable = new MySqlDataAdapter(mysqlCommand);

            DataTable dtTables = new DataTable();

            adapterTable.Fill(dtTables);

            var existTable = dtTables.AsEnumerable().FirstOrDefault(a => a.Field<string>($"Tables_in_{_databaseName}").Equals(tableName));

            query = $"select * from {tableName}";

            OleDbCommand dbfQuery = new OleDbCommand(query, connectionDBF);

            OleDbDataAdapter dbfAdaper = new OleDbDataAdapter(dbfQuery);

            DataTable dbfDatatable = new DataTable();

            dbfAdaper.Fill(dbfDatatable);

            connectionDBF.Close();

            if (existTable == null)
            {
                string[] columnsName = new string[dbfDatatable.Columns.Count];

                int colNameIndex = 0;

                foreach (DataColumn col in dbfDatatable.Columns)
                {
                    columnsName[colNameIndex] = $"{col.ColumnName} VARCHAR(200)";

                    colNameIndex++;
                }

                query = $"CREATE TABLE {tableName}(" + string.Join(",", columnsName) + ");";

                mysqlCommand = connection.CreateCommand();

                mysqlCommand.CommandText = query;

                mysqlCommand.ExecuteNonQuery();
            }

            StringBuilder newSql = new StringBuilder();

            int rowIndex = 0;

            int index = 0;

            newSql.Append($"INSERT INTO {tableName} VALUES ");

            string[] columnsValues;

            string columnValue;

            foreach (DataRow item in dbfDatatable.Rows)
            {
                index = 0;

                columnsValues = new string[dbfDatatable.Columns.Count];

                columnValue = string.Empty;

                foreach (DataColumn col in dbfDatatable.Columns)
                {
                    if (item[col.ColumnName] is null)
                    {
                        columnValue = "null";
                    }
                    else
                    {
                        if (typeof(string) == col.DataType)
                        {
                            columnValue = ((string)item[col.ColumnName]).Replace("'", "\\'").Replace("\"", "\\\"");

                            if (ckStringTrim.Checked)
                                columnValue = columnValue.Trim();

                            columnValue = $"'{columnValue}'";
                        }
                        else if (typeof(DateTime) == col.DataType)
                        {
                            DateTime dateTime = (DateTime)item[col.ColumnName];

                            columnValue = "'" + dateTime.ToString("yyyy-MM-dd") + "'";
                        }
                        else if (typeof(decimal) == col.DataType)
                        {
                            columnValue = ((decimal)item[col.ColumnName]).ToString("0");
                        }
                        else if (typeof(double) == col.DataType)
                        {
                            columnValue = ((double)item[col.ColumnName]).ToString("0");
                        }
                        else
                        {
                            columnValue = "" + item[col.ColumnName];
                        }
                    }

                    columnsValues[index] = columnValue;

                    index++;
                }

                newSql.Append("(" + string.Join(",", columnsValues) + ")");

                if (rowIndex < dbfDatatable.Rows.Count - 1)
                    newSql.AppendLine(",");

                rowIndex++;
            }

            mysqlCommand = connection.CreateCommand();

            mysqlCommand.CommandText = newSql.ToString();

            mysqlCommand.ExecuteNonQuery();

            connection.Close();
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Style = ProgressBarStyle.Blocks;

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, Resources.Shared.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(Resources.Shared.OperationSuccess, Resources.Shared.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
