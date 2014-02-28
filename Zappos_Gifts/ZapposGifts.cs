using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace Zappos_Gifts
{
    public partial class ZapposGifts : Form
    {
        const string key = "52ddafbe3ee659bad97fcce7c53592916a6bfd73"; // Zappos API key to make the calls
        const Int16 maxArray = Int16.MaxValue;                         // Maximum array indexes


        public ZapposGifts()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            #region Variable Declaration
            ProductList[] productList = new ProductList[maxArray];
            ProductCombinationList[] productCombinationList = new ProductCombinationList[maxArray];
            int numberProducts = 0;              // Number of Products
            decimal price = 0;                   // Price for all the products
            //int totalResultCount = 0;          // Max Result Count
            #endregion

            #region Obtain Values
            try
            {
                numberProducts = Convert.ToInt32(txtNumberProducts.Text);
            }
            catch (Exception ex)
            {
                lbProduct.Visible = true;
                lbPrice.Visible = false;
                return;
            }
            Console.WriteLine("\nInform desired price:");
            try
            {
                price = Convert.ToDecimal(txtPrice.Text);
            }
            catch (Exception ex)
            {
                lbPrice.Visible = true;
                lbProduct.Visible = false;
                return;
            }

            if (price == 0)
            {
                lbPrice.Visible = true;
                lbProduct.Visible = false;
                return;
            }
            else
                lbPrice.Visible = false;

            if (numberProducts == 0)
            {
                lbProduct.Visible = true;
                lbProduct.Visible = false;
                return;
            }
            else
                lbProduct.Visible = false;

            #endregion

            #region Obtain List of products grouping by price
            string endPoint = @"http://api.zappos.com/Search?" + "&key=" + key +
                                                                 "&sort={\"price\":\"asc\"}&facets=[\"price\"]&facetSort=name";

            var client = new RestClient(endPoint);
            var json = client.MakeRequest();

            JObject o = JObject.Parse(json);
            int totalProducts = 0; // counter for the Products
            int totalLines = 0;
            string products;
            string[] productsParsed = new string[Int16.MaxValue];
            foreach (KeyValuePair<String, JToken> d in o)
            {
                if (d.Key == "facets")
                {
                    products = Convert.ToString(d.Value);
                    products = products.Replace("\n", "").Replace("\r", "").Replace("[", "").Replace(" ", "").Replace("],", "").Replace("]", "")
                                       .Replace("{", "").Replace("}", "").Replace("\"values\"", "").Replace("\"name\"", "").Replace("\"count\":", "")
                                       .Replace("\"facetFieldDisplayName\":\"\"", "").Replace("}]", "").Replace("\"", "").Replace("facetField:price", "");
                    productsParsed = products.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    totalLines = productsParsed.Length;
                    break;
                }
            }

            int countProduct = 0;
            string[] values = new string[2];

            for (totalProducts = 0; totalProducts < totalLines; totalProducts++)
            {
                values = productsParsed[totalProducts].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); ;
                if (values.Length != 0)
                {
                    ProductList productListObj = new ProductList();
                    productListObj.price = Convert.ToDecimal(values[0] == "" ? "0" : values[0]);
                    productListObj.count = Convert.ToInt16(values[1] == "" ? "0" : values[1]);
                    productList[countProduct] = productListObj;
                    countProduct++;
                }
            }

            #endregion

            #region Obtain best combination of products
            decimal pricePerProduct = price / numberProducts;
            productsParsed = new string[Int16.MaxValue];
            int countProductCombination = 0;
            decimal countPrice = 0;

            // List of prices and count
            for (int i = 0; i < countProduct; i++)
            {
                endPoint = @"http://api.zappos.com/Search?" + "&key=" + key +
                                                                "&excludes=[\"styleId\",\"productUrl\",\"colorId\",\"thumbnailImageUrl\",\"productId\"]" +
                                                                "&limit=100&filters={\"price\":[\"" + Convert.ToString(productList[i].price) + "\"]}";
                var client2 = new RestClient(endPoint);
                var json2 = client2.MakeRequest();

                JObject o2 = JObject.Parse(json2);

                foreach (KeyValuePair<String, JToken> d2 in o2)
                {
                    if (d2.Key == "results")
                    {
                        products = Convert.ToString(d2.Value);
                        products = products.Replace("\n", "").Replace("\r", "").Replace("[", "").Replace(" ", "").Replace("],", "").Replace("]", "")
                                            .Replace("{", "").Replace("}", "").Replace("\"price\"", "").Replace("\"originalPrice\":", "")
                                            .Replace("\"productName\":", "").Replace("\"brandName\":", "").Replace("\"percentOff\":", "")
                                            .Replace("}]", "").Replace("\"", "").Replace("},", "");
                        productsParsed = products.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        productsParsed = products.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries); ;
                        totalLines = productsParsed.Length;
                        break;
                    }
                }

                values = new string[5];

                // List of products for current price
                for (totalProducts = 0; totalProducts < totalLines; totalProducts++)
                {
                    values = productsParsed[totalProducts].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); ;
                    if (values.Length != 0)
                    {
                        ProductCombinationList productCombinationListObj = new ProductCombinationList();
                        productCombinationListObj.price = values[0];
                        productCombinationListObj.originalPrice = values[1];
                        productCombinationListObj.productName = values[2];
                        productCombinationListObj.brandName = values[3];
                        productCombinationListObj.percentOff = values[4];
                        productCombinationList[countProductCombination] = productCombinationListObj;
                        countProductCombination++;
                        countPrice += productList[i].price;
                        if (countProductCombination >= numberProducts && countPrice >= price) // reached max # of products and total price
                        {
                            i = countProduct;
                            break;
                        }
                        else if (countProductCombination >= numberProducts && countPrice < price) // reached max # of products but not the total price
                        {
                            countProductCombination--;
                            countPrice -= productList[i].price;
                            break;
                        }                        
                    }
                }
            }

            #endregion

            #region Print result

            txtOutput.Text = "";
            if (countProductCombination > 0)
            {
                decimal totalPrice = 0;

                for (totalProducts = 0; totalProducts < countProductCombination; totalProducts++)
                {
                    txtOutput.Text += "Product Name: " + productCombinationList[totalProducts].productName + "\r\n";
                    txtOutput.Text += "Brand Name: " + productCombinationList[totalProducts].brandName + "\r\n";
                    txtOutput.Text += "Original Price: " + productCombinationList[totalProducts].originalPrice + "\r\n";
                    txtOutput.Text += "Price: " + productCombinationList[totalProducts].price + "\r\n";
                    txtOutput.Text += "Percent Off: " + productCombinationList[totalProducts].percentOff + "\r\n\r\n";
                    totalPrice += Convert.ToDecimal(productCombinationList[totalProducts].price.Substring(1, productCombinationList[totalProducts].price.Length - 1));
                }
                txtOutput.Text += "Total Price: " + Convert.ToString(totalPrice);
                txtTotal.Text = Convert.ToString(totalPrice);
            }
            else
            {
                txtOutput.Text = "Couldn't find a good combination! Try a different # of products or price!";
                txtTotal.Text = "";
            }

            #endregion
        }
    }

    public class ProductList
    {
        public decimal price { get; set; }
        public int count { get; set; }
    }

    public class ProductCombinationList
    {
        public string price { get; set; }
        public string originalPrice { get; set; }
        public string productName { get; set; }
        public string brandName { get; set; }
        public string percentOff { get; set; }
    }
}
