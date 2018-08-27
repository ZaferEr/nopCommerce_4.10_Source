﻿using Nop.Plugin.ExternalSuppliers.STM.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.ExternalSuppliers.STM.Extensions
{
    public static class VariantExtensions
    {
        internal static void SetSupplierProductId(this VariantData data)
        {
            string brand = data.Brand.ToLower();
            switch (brand)
            {
                case "tatonka":
                    {
                        if (data.SupplierProductId.Contains("-"))
                            data.SupplierProductId = data.SupplierProductId.Substring(0, data.SupplierProductId.IndexOf("-"));
                        break;
                    }
                default:
                    {
                        // This is to fix that STM often has itemnumbers specific for each variant, we need the actual productnumber
                        // We need to check for 2 "-" as Gerber is using 1 "-" in some products not otherwise connected
                        if (data.SupplierProductId.Contains("-") && data.SupplierProductId.IndexOf("-") != data.SupplierProductId.LastIndexOf("-"))
                            data.SupplierProductId = data.SupplierProductId.Substring(0, data.SupplierProductId.IndexOf("-"));

                        break;
                    }
            }
        }

        internal static void SetSizeString(this VariantData data)
        {
            if (string.IsNullOrEmpty(data.SizeStr) && data.OriginalTitle.Contains("\"\""))
            {
                string tmpSize = data.OriginalTitle.Substring(data.OriginalTitle.IndexOf("\"\""));
                tmpSize = tmpSize.Replace("\"", "").Trim();
                data.SizeStr = tmpSize;
            }
        }

        internal static bool MissingSizeOrColor(this VariantData data)
        {
            var firstIndex = data.OrgItemNumber.IndexOf("-");
            // If we do not have "-" in the string, maybe no color and sizes are nessacary
            if (firstIndex == -1)
                return false;

            bool containsTwo = firstIndex != data.OrgItemNumber.LastIndexOf("-");
            // If we do not have "-" twice, maybe no color and sizes are nessacary
            if (!containsTwo)
                return false;

            // Now we know this product should have both size and color strings.            
            string productId = data.OrgItemNumber.Substring(0, data.OrgItemNumber.IndexOf("-"));
            string colorStr = data.OrgItemNumber.Substring(data.OrgItemNumber.IndexOf("-") + 1, (data.OrgItemNumber.LastIndexOf("-") - data.OrgItemNumber.IndexOf("-") - 1));
            string sizeStr = data.OrgItemNumber.Substring(data.OrgItemNumber.LastIndexOf("-") + 1);

            // Now we have a problem, we should have both color and size
            if ((string.IsNullOrEmpty(data.ColorStr) && string.IsNullOrEmpty(data.SizeStr)) || (data.ColorStr == "0" && data.SizeStr == "0"))
                return true;

            // All good, no missing color or size
            return false;
        }
    }
}
