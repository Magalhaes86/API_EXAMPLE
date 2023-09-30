using System;
using XDPeople.Data;
using XDPeople.Business;
using XDPeople.Entities;
using XDPeople.Utils;
using System.Drawing;
using System.Linq;

namespace API_EXAMPLE
{
    class examples
    {

        public static void GenerateDocument(dynamic orderObjectData)
        {

            SalesDocumentManager manager = new SalesDocumentManager(Db.CurrentDatabase, EDocumentType.Sales);

            string documentTypeId = orderObjectData["documentType"];
            int serieId = 1;

            string idClient = orderObjectData["order"].idUser;
            int rows = orderObjectData["nr_order_lines"];

            manager.Init(documentTypeId, serieId);

            EntityProvider p = new EntityProvider();
            Entity entity = p.Get(idClient);

            manager.SetEntity(entity);

            SalesDocumentDetailManager detailManager = manager.DetailManager;

            detailManager.Init(false, false);

            for (int i = 0; i < rows; i++)
            {
                if (i > 0)
                {
                    detailManager.Init();
                }

                string KeyId = orderObjectData["order_lines"][i].reference;
                decimal quantity = orderObjectData["order_lines"][i].quantity;
                decimal price = orderObjectData["order_lines"][i].product_value;

                detailManager.SetItemID(KeyId);
                detailManager.SetQuantity(quantity);
                detailManager.SetPrice(price);

                if (i > 0)
                {
                    detailManager.Commit(false, false);
                }
                else
                {
                    detailManager.Commit(false, true);
                }
            }

            manager.Calculate();

            manager.CurrentDocument["demoapi.id"] = 0;
            manager.CurrentDocument["demoapi.demoapi"] = "TESTE";

            try
            {
                manager.Validate();
            }
            catch (DocumentValidationException ex)
            {
                Console.WriteLine("Document cannot be saved because:");
                Console.WriteLine(ex.Message);
                return;
            }

            try
            {

                if (manager.Save())
                {
                    Console.WriteLine("Sucess");
                }
                else
                {
                    Console.WriteLine("Failed to save document");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving:");
                Console.WriteLine(ex.Message);
            }

        }

        public static void GenerateExternalySignedDocument(dynamic orderObjectData)
        {

            //initialize a new data context for transactions purposes
            DbUniversal dataContext = Db.CurrentDatabase.Clone();

            SalesDocumentManager manager = new SalesDocumentManager(dataContext, EDocumentType.Sales, true);
            SalesDocumentProvider documentProvider = new SalesDocumentProvider(dataContext);
            SalesDocumentDetailManager detailManager = manager.DetailManager;

            string documentTypeId = orderObjectData["documentType"];
            int serieId = 1;

            //validate external serie usage
            XConfigDocumentsSeriesBE serie = GlobalVars.GlobalListXConfigDocumentsSeries.FirstOrDefault(x => x.Id == serieId);

            if (serie == null)
                throw new Exception("Serie not found");

            if (serie.SourceBilling != (int)SourceBilling.I)
                throw new Exception("Invalid Source Billing");


            string idClient = orderObjectData["order"].idUser;
            int rows = orderObjectData["nr_order_lines"];

            //Start a new document
            manager.Init(documentTypeId, serieId);

            EntityProvider p = new EntityProvider();
            Entity entity = p.Get(idClient);

            //Set the customer
            manager.SetEntity(entity);

            for (int i = 0; i < rows; i++)
            {
                //Start a new document line
                detailManager.Init(false, false);

                string KeyId = orderObjectData["order_lines"][i].reference;
                decimal quantity = orderObjectData["order_lines"][i].quantity;
                decimal price = orderObjectData["order_lines"][i].product_value;

                detailManager.SetItemID(KeyId);
                detailManager.SetQuantity(quantity);
                detailManager.SetPrice(price);

                detailManager.Commit(false, true);

            }

            ItemTransactionDocument document = manager.CurrentDocument;

            manager.Calculate();

            //totals
            document.TotalIncome = 0;
            document.TotalTaxes = 0;
            document.LineDiscountAmount = 0;
            document.TotalAmount = 0;
            document.TotalNetAmount = 0;
            document.TotalTaxAmount = document.TotalAmount - document.TotalNetAmount;
            document.HeaderDiscountAmount = 0;

            //certification fields: fill accordingly to original system
            document.CreateDate = DateTime.Now;
            document.OsDate = DateTime.Now;
            document.Number = 1;
            document.TotalAmount = 99999;
            document.SignatureVersionPT = 1;
            document.SignatureHashPT = "dljoqwue932184opn";
            document.SignatureStampPT = XDCrypt.GetInstance().GetSignatureStamp(document.SignatureHashPT);

            try
            {
                manager.Validate();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Document cannot be saved because:");
                Console.WriteLine(ex.Message);
                return;
            }

            try
            {

                if (documentProvider.Save(document, true))
                {
                    Console.WriteLine("Sucess");
                }
                else
                {
                    Console.WriteLine("Failed to save document");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving:");
                Console.WriteLine(ex.Message);
            }

        }

        public static void GenerateReceiptDocument(string entityKeyId)
        {

            if (!XDLicence.IsModuleActive(XDPeople.License.LicenseModules.MssIntegration))
                return;


            //initialize a new data context for transactions purposes
            DbUniversal dataContext = Db.CurrentDatabase.Clone();
            ReceiptDocumentManager manager = new ReceiptDocumentManager(dataContext, "RE", true);

            manager.Init();
            //manager.SetSeries();
            //manager.SetType()

            if (GlobalVars.GlobalDictionaryEntities.ContainsKey(entityKeyId))
            {
                Entity customer = GlobalVars.GlobalDictionaryEntities[entityKeyId];

                manager.SetCustomer(customer);

                manager.CurrentDocument.CreateDate = DateTime.Now;

                foreach (ReceiptDocumentDetail detail in manager.CurrentDocument.Details)
                {
                    manager.SetCurrentDetail(detail);
                    manager.SetMarkedForPaymentFlag(true);
                    manager.SetDetailAmountToPay(detail.Total);
                }

                try
                {
                    PaymentTypeBE payment = GlobalVars.GlobalListPaymentType.FirstOrDefault(x => x.PaymentMechanism == PaymentMechanism.NU.ToString());

                    manager.SetPaymentType(payment);

                    if (manager.Validate(false))
                        manager.Save();
                }
                catch (Exception ex)
                {

                    Log.Write(ex);
                }
            }

        }

        public static void NewCustomer()
        {
            Entity customer = new Entity()
            {
                KeyId = "C00001",
                Name = "Nome do cliente",
                Vat = "123456789",
                Address = "Morada",
                PostalCode = "1100-001",
                City = "Lisboa"
            };

            EntityProvider entityProvider = new EntityProvider();

            entityProvider.Save(customer, true);
            GlobalVars.GlobalListEntity;


        }

        public static void NewItem()
        {
            ItemBE item = new ItemBE()
            {
                ItemType = (int)ItemType.Normal,
                Description = "Descrição",
                Barcode = "1234567890123",
                PurchaseNetPrice = 10m,
                PurchasePrice = 12.3m,
                NetPrice1 = 20m,
                RetailPrice1 = 24.6m
            };

            ItemProvider itemProvider = new ItemProvider();

            itemProvider.Save(item, true);
        }

        public Bitmap GetUserPicture(int userid)
        {
            UserProvider provider = new UserProvider();

            UserBE user = provider.Get(userid);

            return user.Picture;
        }

    }
}
