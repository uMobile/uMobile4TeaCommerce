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

        //foreach (Country c in TC.GetCountries(1)) {
        //    list.AddListItem(new ListItem(c.Name, subtitle: c.RegionCode + " ", icon: GenericIcon.FlagAlt));
        //}

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
                list.AddListItem(new ListItem(order.OrderNumber,
                    subtitle: order.Properties.Get("email") + "<br />" + order.Properties.Get("city") + "<br />" + order.DateCreated + "<br />" + "<b>" + order.TotalPrice.Formatted + "</b>",
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

        Form form = new Form();
        FormFieldset details = new FormFieldset("Order Details", "You can edit the properties of this order");

        form.AddFieldset(details);

        TextField orderNumber = new TextField("orderNumber", "OrderNumber", order.OrderNumber);
        TextField city = new TextField("city", "City", order.Properties.Get("city").ToString());
        TextField price = new TextField("price", "Total price", order.TotalPrice.Formatted);
        DatepickerField dateCreated = new DatepickerField("dateCreated", "Date created", order.DateCreated);
        TextField email = new TextField("email", "Email", order.Properties.Get("email").ToString());
        SelectField status = new SelectField("status", "Status", order.OrderStatusId.ToString());

        status.AddOption("New", "1");
        status.AddOption("Completed", "2");
        status.AddOption("Cancelled", "3");

        details.AddFormItem(orderNumber);
        details.AddFormItem(city);
        details.AddFormItem(price);
        details.AddFormItem(dateCreated);
        details.AddFormItem(email);
        details.AddFormItem(status);

        form.primary = new Button("Save", new Call("OrderSave", new string[] { order.StoreId.ToString(), order.Id.ToString() }));

        return form.UmGo(order.OrderNumber);
    }

    [umMethod(Title = "Save", Visible = false)]
    public static string OrderSave(string storeId, string orderId)
    {
        Order order = TC.GetOrder(Convert.ToInt32(storeId), new Guid(orderId));
        Store store = TC.GetStore(Convert.ToInt32(storeId));

        var orderNumber = Utils.GetPostParameter("orderNumber");
        var city = Utils.GetPostParameter("city");
        var price = Utils.GetPostParameter("price");
        var dateCreated = Utils.GetPostParameter("dateCreated");
        var email = Utils.GetPostParameter("email");
        var status = Utils.GetPostParameter("status");

        order.OrderNumber = orderNumber;
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
}