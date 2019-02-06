using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using System.Collections.ObjectModel;
using DevExpress.Xpf.Grid.LookUp;
using DevExpress.Xpf.Editors.Settings;
using Corasau.Client.Models;
using Corasau.Client.Utilities;
using System.Threading.Tasks;
using System.Collections;
using DevExpress.Xpf.Grid;
using Uniconta.API.Service;
using DevExpress.Xpf.Core;
using DevExpress.Data.Filtering;

namespace Corasau.Client.Pages
{
    public class CorasauDataGridGLAccount : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLAccountClient); }  }
    }

    public partial class Gl_Accounts : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GL_Chart_Accounts; } }
        public Gl_Accounts()
            : base(null)
        {
            InitPage();
            BindGrid();
            ((TableView)dgGLTable.View).RowStyle = Application.Current.Resources["RowStyle"] as Style;
        }

        public Gl_Accounts(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
            BindGrid();
            ((TableView)dgGLTable.View).RowStyle = Application.Current.Resources["RowStyle"] as Style;
        }
        public Gl_Accounts(UnicontaBaseEntity param)
            : base(null)
        {
            InitPage();
            dgGLTable.SetSource(new UnicontaBaseEntity[] { param });
            ((TableView)dgGLTable.View).RowStyle = Application.Current.Resources["RowStyle"] as Style;
        }

        private void InitPage()
        {
            InitializeComponent();
            dgGLTable.RowDoubleClick += dgGLTable_RowDoubleClick;
            this.DataContext = this;
            SetRibbonControl(localMenu, dgGLTable);
            dgGLTable.api = api;
            var Comp = api.CompanyEntity;
            if (Comp != null && !Comp.HasDecimals)
                PrevYear.HasDecimals = PrevYearDebit.HasDecimals = PrevYearCredit.HasDecimals = 
                ThisYear.HasDecimals = ThisYearDebit.HasDecimals = ThisYearCredit.HasDecimals = false;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            Utility.Refresh += Utility_Refresh;
            dgGLTable.BusyIndicator = busyIndicator;
            this.BeforeClose += Gl_Accounts_BeforeClose;
            ribbonControl.DisableButtons(new string[] { "AddRow", "CopyRow", "DeleteRow", "SaveGrid" });
        }

        protected override void OnLayoutLoaded()
        {
            setDim();
        }

        void dgGLTable_RowDoubleClick()
        {
           localMenu_OnItemClicked("GLTran");
        }

        void Gl_Accounts_BeforeClose()
        {
            Utility.Refresh -= Utility_Refresh;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            GLAccountClient selectedItem = dgGLTable.SelectedItem as GLAccountClient;
            switch (ActionType)
            {
                case "EditAll":
                    if(dgGLTable.Visibility==Visibility.Visible)
                        EditAll();
                    break;
                case "AddItem":
                    AddDockItem(TabControls.GL_AccountPage2, api, Uniconta.ClientTools.Localization.lookup("Accounts"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditItem":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GL_AccountPage2, selectedItem);
                    break;
                case "AddRow":
                    if (dgGLTable.Readonly)
                        AddDockItem(TabControls.GL_AccountPage2, api, Uniconta.ClientTools.Localization.lookup("Accounts"), ";component/Assets/img/Add_16x16.png");
                    else
                        dgGLTable.AddRow();
                    break;
                case "CopyRow":
                    dgGLTable.CopyRow();
                    break;
                case "DeleteRow":
                    dgGLTable.DeleteRow();
                    break;
                case "SaveGrid":
                    dgGLTable.SaveData();
                    break;
                case "AllocationAccounts":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GL_AllocationsAccounts, dgGLTable.syncEntity);
                    break;
                case "GLTran":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.TransactionReport, dgGLTable.syncEntity);
                    break;
                case "Trans":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.AccountsTransaction, dgGLTable.syncEntity, string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Name));
                    break;
                case "Budget":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.GLBudgetLinePage, dgGLTable.syncEntity);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgGLTable.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgGLTable.syncEntity);
                    break;
                case "VatOnClient":
                    if (selectedItem == null) return;
                    AddDockItem(TabControls.VatOnClientsReport, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("VATprDC"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgGLTable.Readonly)
            {
                dgGLTable.MakeEditable();
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddRow", "CopyRow", "DeleteRow", "SaveGrid" });
                editAllChecked = false;
            }
            else
            {
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                    {
                        if (confirmationDialog.DialogResult == null)
                            return;

                        switch (confirmationDialog.ConfirmationResult)
                        {
                            case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                await dgGLTable.SaveData();
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                break;
                        }
                        editAllChecked = true;
                        dgGLTable.Readonly = true;
                        dgGLTable.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddRow", "CopyRow", "DeleteRow", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgGLTable.Readonly = true;
                    dgGLTable.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddRow", "CopyRow", "DeleteRow", "SaveGrid" });
                }
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgGLTable.HasUnsavedData;
            }
        }
        void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GL_AccountPage2)
                dgGLTable.UpdateItemSource(argument);
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgGLTable.UpdateItemSource(argument);
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgGLTable.Filter(propValuePair);
        }

        private void BindGrid()
        {
            var t = Filter(null);
        }

        void setDim()
        {
            Corasau.Client.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glAccount = (sender as Image).Tag as GLAccountClient;
            if(glAccount!=null)
                AddDockItem(TabControls.UserDocsPage, glAccount);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glAccount = (sender as Image).Tag as GLAccountClient;
            if (glAccount != null)
                AddDockItem(TabControls.UserNotesPage, glAccount);
        }
    }
}
