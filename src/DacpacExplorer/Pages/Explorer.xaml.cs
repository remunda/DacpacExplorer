﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DacpacExplorer.External;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Controls;
using FirstFloor.ModernUI.Windows.Navigation;
using Microsoft.SqlServer.Dac.Model;

namespace DacpacExplorer.Pages
{
    public partial class Explorer : Page, IContent
    {
        private TSqlModel _model;
        private RecursionGuard _recursionGuard;

        public Explorer()
        {
            InitializeComponent();
            TreeView.SelectedItemChanged += TreeView_SelectedItemChanged;
        }

        public void Display()
        {
            _recursionGuard = new RecursionGuard();
            TreeView.Items.Clear();
            

            var repository = ModelRepository.GetRepository();
            _model = repository.GetModel();

            var propertiesPageBuilder = new PropertiesPageBuilder();

            var dacpacNode = AddRootTreeItem("Dacpac Properties");
            dacpacNode.Tag = new CachedObjectDisplay()
            {
                Properties = propertiesPageBuilder.GetPropertiesDisplayForDacpac(_model),
                Script = ""
            };

            if (repository.ValidateModel())
            {
                var messages = _model.Validate();
                var messagesNode = AddTreeItem("Validation Result", dacpacNode);
                messagesNode.Tag = new CachedObjectDisplay()
                {
                    Properties = propertiesPageBuilder.GetPropertiesDisplayForValidationMessages(messages),
                    Script = ""
                };

            }


            DisplayTopLevelNode("Tables", ModelSchema.Table);
            DisplayTopLevelNode("Views", ModelSchema.View);

            var programabililtyNode = AddRootTreeItem("Programmability");
            DisplayTopLevelNode(programabililtyNode, "Procedures", ModelSchema.Procedure);
            DisplayTopLevelNode(programabililtyNode, "Scalar Functions", ModelSchema.ScalarFunction);
            DisplayTopLevelNode(programabililtyNode, "Table-Valued Functions", ModelSchema.TableValuedFunction);
            DisplayTopLevelNode(programabililtyNode, "Database Triggers", ModelSchema.DatabaseDdlTrigger);
            DisplayTopLevelNode(programabililtyNode, "Server Triggers", ModelSchema.ServerDdlTrigger);
            DisplayTopLevelNode(programabililtyNode, "Assemblies", ModelSchema.Assembly);

            var securityNode = AddRootTreeItem("Security");
            DisplayTopLevelNode(securityNode, "Server Logins", ModelSchema.Login);
            DisplayTopLevelNode(securityNode, "Database Users", ModelSchema.User);
            DisplayTopLevelNode(securityNode, "Schemas", ModelSchema.Schema);
            DisplayTopLevelNode(securityNode, "Roles", ModelSchema.Role);

            Cursor = Cursors.Arrow;

        }

        void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Properties.Children.Clear();
            ScriptDisplay.Text = "";

            var item = e.NewValue as TreeViewItem;
            if (null == item)
            {
                return;
            }

            var display = item.Tag as CachedObjectDisplay;

            if (null == display)
                return;

            Properties.Children.Add(display.Properties);
            ScriptDisplay.Text = display.Script;

        }

        private void DisplayTopLevelNode(ItemsControl rootNode, string header, ModelTypeClass type)
        {
            var newNode = new TreeViewItem() { Header = header };

            var objects = _model.GetObjects(DacQueryScopes.All, type);

            foreach (var child in objects)
            {
                var childTreeNode = AddTreeItem(child.Name.ToString(), newNode);
                DisplayObject(header, child, childTreeNode);
            }

            if (newNode.Items.Count > 0)
                rootNode.Items.Add(newNode);
        }

        private void DisplayTopLevelNode(string description, ModelTypeClass type)
        {
            DisplayTopLevelNode(TreeView, description, type);
        }

        private void DisplayObject(string rootNodeHeader, TSqlObject currentObject, TreeViewItem currentObjectTreeViewItem)
        {
         
            var propertiesPageBuilder = new PropertiesPageBuilder();
            var properties = propertiesPageBuilder.GetPropertiesDisplay(currentObject);
            var script = GetScript(currentObject);

            currentObjectTreeViewItem.Tag = new CachedObjectDisplay()
            {
                Properties = properties,
                Script = script
            };
            
            var childObjectTypes = new Dictionary<string, TreeViewItem>();

            DisplyChildObjects(rootNodeHeader, currentObjectTreeViewItem, currentObject.GetChildren(), childObjectTypes);           

        }


        private void DisplyChildObjects(string rootNodeHader, TreeViewItem currentObjectTreeViewItem, IEnumerable<TSqlObject> children, Dictionary<string, TreeViewItem> childObjectTypes)
        {
            foreach (var child in children.OrderBy(p=>p, new SqlObjectComparer()))
            {
                var type = child.ObjectType.Name;
                var typeContainerHeader = GetContainerHeader(type);

                if (!childObjectTypes.ContainsKey(typeContainerHeader))
                {
                    var item = AddTreeItem(typeContainerHeader, currentObjectTreeViewItem);
                    childObjectTypes.Add(typeContainerHeader, item);
                }

                var childTreeViewItem = AddTreeItem(child.Name.ToString(), childObjectTypes[typeContainerHeader]);

                DisplayObject(rootNodeHader, child, childTreeViewItem);
            }
        }

        private static string GetContainerHeader(string type)
        {

            if (type.EndsWith("ex", StringComparison.OrdinalIgnoreCase))
                return type + "es";

            if (type.EndsWith("ty", StringComparison.OrdinalIgnoreCase))
                return type.Replace("ty", "ties");

            return type + "s";
            
        }

        private string GetScript(TSqlObject table)
        {
            var script = "";

            if (!table.TryGetScript(out script))
            {
                script = "Object doesn't have script, see top level object instead";
            }

            return script;
        }

       
        
        private TreeViewItem AddRootTreeItem(string header)
        {
            return AddTreeItem(header, TreeView);
        }

        private TreeViewItem AddTreeItem(string header, ItemsControl parent)
        {
            if (string.IsNullOrEmpty(header))
            {
                header = "Unnamed";
            }

            var item = new TreeViewItem() { Header = header };
            parent.Items.Add(item);
            return item;
        }

        public void OnFragmentNavigation(FragmentNavigationEventArgs e)
        {
            
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            
        }

        public void OnNavigatedTo(NavigationEventArgs e)
        {
            Display();            
        }

        public void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            TreeView.Items.Clear();
        }
    }
}
