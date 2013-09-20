using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using umbraco.BusinessLogic;

using TeaCommerce.Api.Models;
using TeaCommerce.Umbraco.Web;


namespace lecoati.uMobile.Extensions
{
    public static class TCExtensions
    {
        public static Dictionary<int, OrderStatus> GetOrderStatuses() {

            string sql = "SELECT Id, StoreId, Name, IsDeleted FROM TeaCommerce_OrderStatus";
            umbraco.DataLayer.IRecordsReader rd;
            
            Dictionary<int, OrderStatus> orderStatuses = new Dictionary<int, OrderStatus>();

            rd = Application.SqlHelper.ExecuteReader(sql);

            while (rd.Read())
            {
                orderStatuses.Add(Convert.ToInt32(rd.GetLong("Id")), new OrderStatus(rd.GetLong("StoreId"), rd.GetString("Name")));
            }

            return orderStatuses;
        }

        public static OrderStatus GetOrderStatus(long storeId, long id)
        {

            string sql = "SELECT Id, StoreId, Name, IsDeleted FROM TeaCommerce_OrderStatus WHERE StoreId = " + storeId + " AND Id = " + id;
            umbraco.DataLayer.IRecordsReader rd;

            rd = Application.SqlHelper.ExecuteReader(sql);

            if (rd.Read())
            {
                return new OrderStatus(rd.GetLong("StoreId"), rd.GetString("Name"));
            }

            return null;
        }
    }
}
