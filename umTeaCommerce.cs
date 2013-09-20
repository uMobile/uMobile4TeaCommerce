using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using lecoati.uMobile;
using lecoati.uMobile.umCore;
using lecoati.uMobile.umComponents;

using TeaCommerce.Umbraco.Web;
using TeaCommerce.Api.Models;
using System.Xml.Linq;
using lecoati.uMobile.umHelpers;
using System.Net;
using System.Collections.Specialized;
using System.Web.Helpers;
using System.Text;

namespace lecoati.uMobile.Extensions
{
    /// <summary>
    /// Summary description for umTeaCommerce
    /// </summary>
    [umClass(Category = "Tea Commerce", Icon = GenericIcon.Money)]
    public class umTeaCommerce : uMobile
    {
        [umMethod]
        public static string Orders()
        {
            List list = new List();

            Store currentStore = TC.GetStore(1);
            XElement allOrders = TC.GetAllFinalizedOrdersAsXml(1);

            var ordersList = (from order in allOrders.Elements("order")
                              orderby DateTime.Parse(order.Attribute("dateCreated").Value)
                              descending
                              select order.Attribute("id").Value);

            if (ordersList.Any())
            {
                foreach (var orderId in ordersList)
                {
                    Order order = TC.GetOrder(1, new Guid(orderId));
                    list.AddListItem(new ListItem(order.OrderNumber + "<b style='float: right;'>" + order.TotalPrice.Formatted + "</b>",
                        subtitle: "<span style='float: right;'>" + order.Properties.Get("email") + "</span>" + TCExtensions.GetOrderStatus(order.StoreId, order.OrderStatusId).Name.ToString() + "<br /><span style='float: right;'>" + order.Properties.Get("city") + "</span>" + order.DateCreated,
                        icon: GenericIcon.ShoppingCart,
                        action: new Call("OrderInfo", new string[] { order.StoreId.ToString(), order.Id.ToString() })
                    ));
                }
            }

            return list.UmGo();
        }

        [umMethod(Visible = false)]
        public static string OrderInfo(string storeId, string orderId)
        {
            Order order = TC.GetOrder(Convert.ToInt32(storeId), new Guid(orderId));
            Store store = TC.GetStore(Convert.ToInt32(storeId));

            List orderList = new List();

            string statusName = TCExtensions.GetOrderStatus(order.StoreId, order.OrderStatusId).Name.ToString();
            //bool positiveStatus = PositiveWord(statusName);

            //if (positiveStatus)
            //{
            //    statusName = "<b style='color: green;'>" + statusName + "</b>";
            //}
            //else
            //{
            //    statusName = "<b style='color: red;'>" + statusName + "</b>";
            //}
            
            ListItem detailsRow = new ListItem(
                title:
                    order.OrderNumber + "<br />",
                subtitle:
                    "City: " + order.Properties.Get("city").ToString() + "<br />" +
                    "Total Price: " + order.TotalPrice.Formatted + "<br />" +
                    "Date Created: " + order.DateCreated + "<br /><br />" +
                    "<span style='font-size: 1.3em; margin-top: 20px;'>" + "Email: <a href='mailto:" + order.Properties.Get("email").ToString() + "'>" + order.Properties.Get("email").ToString() + "</a></span><br />" +
                    "<span style='font-size: 1.3em;'>" + "Phone: " + order.Properties.Get("phone").ToString() + "</span>" + 
                    "<br />" +
                    "<span style='font-size: 1.3em;'>Status: " + statusName + "</span>"
            );

            orderList.AddListItem(detailsRow);

            orderList.AddListItem(new ListItem(
                title: "Change Order Status",
                icon: GenericIcon.Tag,
                action: new Call("ChangeOrderStatus", new string[] { storeId, orderId })
            ));

            //This is not working at the moment because I was not able to find out the way to get the emailtemplates from the API
            //orderList.AddListItem(new ListItem(
            //    title: "Send Email",
            //    icon: GenericIcon.EnvelopeAlt
            //));

            return orderList.UmGo(order.OrderNumber);
        }

        [umMethod(Title = "Change Order Status", Visible = false)]
        public static string ChangeOrderStatus(string storeId, string orderId)
        {
            Order order = TC.GetOrder(Convert.ToInt32(storeId), new Guid(orderId));
            Store store = TC.GetStore(Convert.ToInt32(storeId));

            Form orderForm = new Form();
            FormFieldset statusFieldset = new FormFieldset();
            orderForm.AddFieldset(statusFieldset);
            
            SelectField status = new SelectField("status", "Status", order.OrderStatusId.ToString());

            foreach (KeyValuePair<int, OrderStatus> orderStatus in TCExtensions.GetOrderStatuses()) {
                status.AddOption(orderStatus.Value.Name, orderStatus.Key.ToString());
            }

            statusFieldset.AddFormItem(status);

            orderForm.primary = new Button("Save", new Call("OrderSave", new string[] { order.StoreId.ToString(), order.Id.ToString() }));

            return orderForm.UmGo(order.OrderNumber);
        }

        [umMethod(Title = "Save", Visible = false)]
        public static string OrderSave(string storeId, string orderId)
        {
            Order order = TC.GetOrder(Convert.ToInt32(storeId), new Guid(orderId));
            Store store = TC.GetStore(Convert.ToInt32(storeId));

            var status = Utils.GetPostParameter("status");

            order.OrderStatusId = Convert.ToInt32(status);

            try
            {
                order.Save();
            }
            catch (Exception ex)
            {
                return (new MessageBox("Order couldn't be saved. Exception ocurred: <br />" + ex.Message + "<br/>" + ex.InnerException)).UmGo();
            }

            return (new MessageBox("Order edited successfully")).UmGo();
        }

        //Not working. TODO: Find a way to get the TeaCommerce email templates from the API and send it to the costumers.
        [umMethod(Title = "Send Email", Visible = false)]
        public static string SendEmail(string storeId, string orderId)
        {
            Order order = TC.GetOrder(Convert.ToInt32(storeId), new Guid(orderId));
            Store store = TC.GetStore(Convert.ToInt32(storeId));

            TeaCommerce.Umbraco.Application.Trees.Tasks.EmailTemplateTask emailTask = new TeaCommerce.Umbraco.Application.Trees.Tasks.EmailTemplateTask();
            emailTask.Alias = "confirmationEmail";
            emailTask.TypeID = 1;

            long? templateId = store.ConfirmationEmailTemplateId;

            if (emailTask.Save()) {
                return (new MessageBox("Email sended.")).UmGo();
            };

            return (new MessageBox("Error on sending email")).UmGo();
        }

        public static bool PositiveWord(string text) {
            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["text"] = text;
                data["service"] = "sentiment_news";

                byte[] responseArray = wb.UploadValues("http://nlptools.atrilla.net/api/", "POST", data);
                string response = Encoding.ASCII.GetString(responseArray);

                var decodedResponse = Json.Decode(response);

                decimal NEG = decodedResponse.likelihood.NEG;
                decimal POS = decodedResponse.likelihood.POS;
                decimal NEU = decodedResponse.likelihood.NEU;

                return (POS > NEG);
            }
        }
    }
}