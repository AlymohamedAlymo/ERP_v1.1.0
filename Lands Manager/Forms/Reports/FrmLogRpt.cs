﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ComponentFactory.Krypton.Toolkit;
using DgvFilterPopup;
using System.Xml.Serialization;
using System.IO;

namespace DoctorERP
{
    public partial class FrmLogRpt : KryptonForm
    {
        DgvFilterManager dgvManager;
        string ReportTitle = string.Empty;

        bool FullLoading = false;

        public FrmLogRpt(string ReportTitle)
        {
            InitializeComponent();

            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            this.Text = ReportTitle;
            this.ReportTitle = ReportTitle;


            DataGridMain.MouseDoubleClick += DataGridMain_MouseDoubleClick;
            DataGridMain.MouseDown += DataGridMain_MouseDown;

            DataGridMain.RowsAdded += DataGridMain_RowsAdded;
            DataGridMain.RowsRemoved += DataGridMain_RowsRemoved;
        }

        private void FrmReport_Load(object sender, EventArgs e)
        {
            InitGrid();
            FillGrid();
        }


        void FillGrid()
        {





            dgvManager.ActivateAllFilters(false);

            tbLog.Fill();
            DataGridMain.DataSource = tbLog.dtData;


            SetColumnsFilter(DataGridMain);
            DataGridColumns.SetCustomView(DataGridMain, ReportTitle);


        }

        void InitGrid()
        {
            tbLog.Fill("Guid", Guid.Empty);
            DataGridMain.DataSource = tbLog.dtData;

            DataGUIAttribute.FillGrid(DataGridMain, typeof(tbLog));

            dgvManager = new DgvFilterManager(DataGridMain);

            SetColumnsFilter(DataGridMain);

            CalcTotal();
        }



        #region DataGrid Keys
        private void DataGridMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //try
            //{
            //    Guid guid = (Guid)DataGridMain["guid", DataGridMain.CurrentRow.Index].Value;
            //    FrmTable frmtable = new FrmTable(guid);
            //    frmtable.ShowDialog();
            //}
            //catch
            //{

            //}
        }

        private void DataGridMain_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    DataGridView.HitTestInfo hit = DataGridMain.HitTest(e.X, e.Y);
                    DataGridMain.CurrentCell = DataGridMain[hit.ColumnIndex, hit.RowIndex];
                }
            }
            catch
            {

            }
        }


        void DataGridMain_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            CalcTotal();
        }

        void DataGridMain_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            CalcTotal();
        }
        #endregion

        private void CalcTotal()
        {
            //float amountin = CalcColumnTotal(DataGridMain, DataGridMain.Columns["amountin"]);
            //float amountout = CalcColumnTotal(DataGridMain, DataGridMain.Columns["amountout"]);
            //LblBalance.Text = string.Format("الرصيد {0:" + DataGUIAttribute.CurrencyFormat + "}", amountin - amountout);
            //LblAmountIn.Text = string.Format("مقبوضات {0:" + DataGUIAttribute.CurrencyFormat + "}", amountin );
            //LblAmountOut.Text = string.Format("مدفوعات {0:" + DataGUIAttribute.CurrencyFormat + "}", amountin);
        }

        private float CalcColumnTotal(DataGridView datagrid, DataGridViewColumn dgvcolumn)
        {
            float total = 0;
            foreach (DataGridViewRow dr in datagrid.Rows)
            {
                if (dr.Visible)
                {
                    float val = 0;
                    float.TryParse(dr.Cells[dgvcolumn.Name].Value.ToString(), out val);
                    total += val;
                }
            }
            return total;

        }

        #region Custom View
        void SetColumnsFilter(DataGridView datagridview)
        {
            foreach (DataGridViewColumn datacolumn in DataGridMain.Columns)
            {
                if (datacolumn.ValueType.Equals(typeof(Int32)))
                    dgvManager[datacolumn.Name] = new DgvNumRangeColumnFilter();

                if (datacolumn.ValueType.Equals(typeof(Double)))
                    dgvManager[datacolumn.Name] = new DgvNumRangeColumnFilter();

                if (datacolumn.ValueType.Equals(typeof(DateTime)))
                    dgvManager[datacolumn.Name] = new DgvDateRangeColumnFilter();

                if (datacolumn.ValueType.Equals(typeof(String)))
                    dgvManager[datacolumn.Name] = new DgvTextBoxColumnFilter();
            }
        }

        private void MenuExportToExcel_Click(object sender, EventArgs e)
        {



            ExcelXLSX.ExportToExcel(DataGridMain);
        }

        private void MenuCustomView_Click(object sender, EventArgs e)
        {

            dgvManager.ActivateAllFilters(false);

            FrmCustomView frmcustomview = new FrmCustomView(ReportTitle, DataGridMain);
            if (frmcustomview.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                DataGridColumns.SetCustomView(DataGridMain, ReportTitle);
            else
                this.Close();
        }
        #endregion

        #region search Field
        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
                SearchGrid(TxtSearch.Text);
            else if (e.KeyData == Keys.Delete)
            {
                TxtSearch.Text = string.Empty;
                SearchGrid(string.Empty);
            }
        }

        void SearchGrid(string SearchValue)
        {
            foreach (DataGridViewRow row in DataGridMain.Rows)
            {
                row.Visible = true;
            }

            if (SearchValue.Trim().Length <= 0)
            {
                CalcTotal();
                return;
            }

            foreach (DataGridViewRow row in DataGridMain.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (DataGridMain.Columns[cell.ColumnIndex].Visible)
                    {
                        string cellvalue = cell.Value.ToString();
                        if (cellvalue.Trim().ToLower().Contains(SearchValue.Trim().ToLower()))
                        {
                            row.Visible = true;
                            break;
                        }
                        else
                        {
                            CurrencyManager cm = (CurrencyManager)BindingContext[DataGridMain.DataSource];
                            cm.SuspendBinding();
                            row.Visible = false;
                            cm.ResumeBinding();
                        }
                    }
                }
            }
            CalcTotal();
        }

        private void BtnClearSearch_Click(object sender, EventArgs e)
        {
            TxtSearch.Text = string.Empty;
            SearchGrid(string.Empty);
        }
        #endregion

        #region report and print
        private bool Readyreport(FastReport.Report rpt)
        {
            Reports.GenerateReport(ReportTitle, DataGridMain, dgvManager.mBoundDataView.ToTable());

            Reports.InitReport(rpt, string.Empty, false);

            rpt.SetParameterValue("UserName", FrmMain.currentuser.name);

            rpt.RegisterData(dgvManager.mBoundDataView.ToTable(), "data");

            return true;
        }

        private void MenuDesign_Click(object sender, EventArgs e)
        {
            FastReport.Report report = new FastReport.Report();
            if (Readyreport(report))
                Reports.DesignReport(report);
        }

        private void MenuPreview_Click(object sender, EventArgs e)
        {
            FastReport.Report report = new FastReport.Report();
            if (Readyreport(report))
                report.Show(true);
        }

        private void MenuPrint_Click(object sender, EventArgs e)
        {
            FastReport.Report report = new FastReport.Report();
            if (Readyreport(report))
                report.Print();
        }

        #endregion

        private void FrmLogRpt_Shown(object sender, EventArgs e)
        {
            FullLoading = true;
        }

        private void BtnDeleteLog_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(("هل أنت متأكد من مسح السجل"), Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.No)
                return;

            DBConnect.StartTransAction();
            tbLog log = new tbLog();
            log.DeleteALL();
            if (DBConnect.CommitTransAction())
            {
                FrmConfirm cm = new FrmConfirm();
                cm.ShowDialog();
            }
        }
    }
}
