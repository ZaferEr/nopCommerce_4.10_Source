﻿using Newtonsoft.Json;
using Nop.Core.Configuration;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.POS.Kaching.Models;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Nop.Plugin.POS.Kaching
{
    public class POSKachingService
    {       
        private POSKachingSettings _posKachingSettings;
        private readonly IPictureService _pictureService;
        private readonly IProductAttributeService _productAttributeService;

        public bool TestConnection
        {
            get
            {
                return false;
            }
        }

        public POSKachingService(POSKachingSettings kachingSettings, IPictureService pictureService, IProductAttributeService productAttributeService)
        {           
            this._posKachingSettings = kachingSettings;
            this._pictureService = pictureService;
            this._productAttributeService = productAttributeService;
        }

        public void SaveProduct(string json)
        {
            //var test = _settingService.GetSettingByKey<POSKachingSettings>("Test");

            var posKaChingAccountToken = _posKachingSettings.POSKaChingAccountToken;

            var test = "";
            //if(string.IsNullOrEmpty(posKaChingAccountToken.Value))
            //{
            //    _posKachingSettings.POSKaChingAccountToken = "dd";
            //    _settingService.SetSetting("POSKaChingAccountToken", _posKachingSettings.POSKaChingAccountToken);
            //}
            

            
        }

        public string BuildJSONString(Core.Domain.Catalog.Product product)
        {
            KachingProductModel kaChingProduct = new KachingProductModel();

            kaChingProduct.Product = new Models.Product();
            kaChingProduct.Product.Id = product.Id.ToString();
            kaChingProduct.Product.Name = new Description();
            kaChingProduct.Product.Name.Da = kaChingProduct.Product.Name.En = product.Name;
            kaChingProduct.Product.Description = new Description();
            kaChingProduct.Product.Description.Da = kaChingProduct.Product.Description.En = product.FullDescription;
            kaChingProduct.Product.RetailPrice = (long)product.Price;

            foreach (var pp in product.ProductPictures)
            {
                var pictureUrl = _pictureService.GetPictureUrl(pp.PictureId);
                kaChingProduct.Product.ImageUrl = pictureUrl;

                break;
            }

            List<Variant> variants = new List<Variant>();
            List<Dimension> dimensions = GetDimensions(product, ref variants);

            if (variants.Count == 1)
            {
                kaChingProduct.Product.Barcode = variants[0].Barcode;
            }
            else if (variants.Count > 0)
            {
                kaChingProduct.Product.Variants = variants.ToArray();
                kaChingProduct.Product.Dimensions = dimensions.ToArray();
            }

            kaChingProduct.Metadata = new Metadata();
            kaChingProduct.Metadata.Channels = new Channels();
            kaChingProduct.Metadata.Markets = new Markets();
            kaChingProduct.Metadata.Channels.Pos = true;
            kaChingProduct.Metadata.Channels.Online = true;
            kaChingProduct.Metadata.Markets.Dk = true;

            kaChingProduct.Product.Tags = SetTags(product);

            string output = JsonConvert.SerializeObject(kaChingProduct, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
            return output;
        }

        private Tags SetTags(Core.Domain.Catalog.Product product)
        {
            Tags tags = new Tags();

            if (product.ProductCategories != null && product.ProductCategories.Count > 0)
            {
                foreach (ProductCategory cat in product.ProductCategories)
                {
                    switch (cat.Category?.Name)
                    {
                        case "Mens clothing": tags.Herretoj = true; break;
                        case "Womens clothing": tags.Dametoj = true; break;
                        case "Child wear": tags.Bornetoj = true; break;
                        case "Back packs": tags.Rygsaekke = true; break;
                        case "Sleeping bags": tags.Soveposer = true; break;
                        case "Tents": tags.Telte = true; break;
                        case "Cooking": tags.Kogegrej = true; break;
                        case "Travel accessories": tags.Tilbehor = true; break;
                        case "Footwear": tags.Fodtoj = true; break;
                        default: tags.Diverse = true; break;
                    }
                }
            }

            return tags;
        }

        private List<Dimension> GetDimensions(Core.Domain.Catalog.Product product, ref List<Variant> variants)
        {
            List<Dimension> dimensions = new List<Dimension>();
            var combinationValues = _productAttributeService.GetAllProductAttributeCombinations(product.Id);

            int colorAttributeId = 0, sizeAttributeId = 0;
            GetColorAndSizeId(ref colorAttributeId, ref sizeAttributeId);

            Variant variant = null;

            foreach (var combinationValue in combinationValues)
            {
                XmlDocument attributesXml = new XmlDocument();
                attributesXml.LoadXml(combinationValue.AttributesXml);

                Value colorValue = null, sizeValue = null;

                foreach (XmlNode node in attributesXml.DocumentElement)
                {
                    Dimension colorDimension = new Dimension();
                    Dimension sizeDimension = new Dimension();
                    List<Value> colorValues = new List<Value>();
                    List<Value> sizeValues = new List<Value>();

                    var attributeId = Convert.ToInt32(node.Attributes["ID"].Value);
                    var attributeValueId = Convert.ToInt32(node.FirstChild.FirstChild.InnerText);
                    var attributeValue = _productAttributeService.GetProductAttributeValueById(attributeValueId);

                    var mapping = _productAttributeService.GetProductAttributeMappingById(attributeId);
                    string imageUrl = "";
                    if (attributeValue.PictureId > 0)
                    {
                        imageUrl = _pictureService.GetPictureUrl(attributeValue.PictureId);
                    }
                    else if (product.ProductPictures != null && product.ProductPictures.Count > 0)
                    {
                        imageUrl = _pictureService.GetPictureUrl(product.ProductPictures.First().PictureId);
                    }

                    if (mapping.ProductAttributeId == colorAttributeId)
                    {
                        colorDimension.Id = "color";
                        colorDimension.Name = "Color";

                        colorValue = new Value();
                        colorValue.Id = attributeValue.Id.ToString();
                        colorValue.ImageUrl = imageUrl;
                        colorValue.Name = attributeValue.Name;

                        colorValues.Add(colorValue);

                        colorDimension.Values = colorValues.ToArray();
                        dimensions.Add(colorDimension);
                    }
                    else if (mapping.ProductAttributeId == sizeAttributeId)
                    {
                        sizeDimension.Id = "size";
                        sizeDimension.Name = "Size";

                        sizeValue = new Value();
                        sizeValue.Id = attributeValue.Id.ToString();
                        sizeValue.Name = attributeValue.Name;

                        sizeValues.Add(sizeValue);

                        sizeDimension.Values = sizeValues.ToArray();
                        dimensions.Add(sizeDimension);
                    }

                    variant = new Variant();
                    variant.Barcode = combinationValue.Gtin;
                    variant.Id = combinationValue.Id.ToString();
                    variant.ImageUrl = imageUrl;
                    variant.DimensionValues = new DimensionValues();

                    variant.DimensionValues.Color = colorValue != null ? colorValue.Id : "0";
                    variant.DimensionValues.Size = sizeValue != null ? sizeValue.Id : "0";

                    variants.Add(variant);
                }
            }

            return dimensions;
        }

        private void GetColorAndSizeId(ref int colorAttributeId, ref int sizeAttributeId)
        {
            foreach (var att in _productAttributeService.GetAllProductAttributes())
            {
                if (att.Name.ToUpper() == "COLOR")
                {
                    colorAttributeId = att.Id;
                }
                else if (att.Name.ToUpper() == "SIZE")
                {
                    sizeAttributeId = att.Id;
                }
            }
        }

    }
}
