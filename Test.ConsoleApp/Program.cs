using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Crm;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Test.ConsoleApp
{
    class Program
    {
        /// <summary>
        /// Добавление средств связи
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="service"></param>
        /// <param name="type">true - email, false - phone</param>
        /// <param name="main">Основной?</param>
        private static void AddCommunication(Entity entity, IOrganizationService service, bool type, bool main)
        {
            try
            {
                Entity communicationToCreate = new Entity("nav_communication");
                communicationToCreate["nav_name"] = (type ? "email: " : "phone: ") + entity.GetAttributeValue<string>("fullname");
                communicationToCreate[type ? "nav_email" : "nav_phone"] = entity.GetAttributeValue<string>(type ? "emailaddress1" : "telephone1");
                communicationToCreate["nav_contactid"] = new EntityReference(entity.LogicalName, entity.Id);
                communicationToCreate["nav_type"] = type;
                communicationToCreate["nav_main"] = main;
                var id = service.Create(communicationToCreate);
                Logger.Log.Info("Создано средство связи " + communicationToCreate["nav_name"]);
            }
            catch(Exception e)
            {
                Logger.Log.Error(e.Message);
            }

        }

        private static void AddContatInfo(Entity entity, IOrganizationService service, bool type)
        {
            try
            {
                string Attr = type ? "nav_email" : " nav_phone";
                Entity contactToUpdate = new Entity("contact", "contactid", entity.GetAttributeValue<AliasedValue>("c.contactid").Value);
                contactToUpdate["telephone1"] = entity.GetAttributeValue<string>(Attr);
                service.Update(contactToUpdate);
                Logger.Log.Info("Добавлен"+(type?"а электронная почта":" номер") + contactToUpdate["nav_name"]);
            }
            catch(Exception e)
            {
                Logger.Log.Error(e.Message);
            }
        }
        static void Main(string[] args)
        {
            Logger.InitLogger();
            Logger.Log.Info("Старт");            
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var connectingString = "AuthType=OAuth; Url=https://trialtest.crm4.dynamics.com/; Username=admin@november1414.onmicrosoft.com; Password=Kik12345; RequireNewInstance=true; AppId=51f81489-12ee-4a9e-aaae-a2591f45987d; RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto; ";
            CrmServiceClient client = new CrmServiceClient(connectingString);
            if(client.LastCrmException!=null)
            {
                Logger.Log.Error(client.LastCrmException);
            }

            var service = (IOrganizationService)client;
            //Обновляем контакты данными из "Средств связи"
            QueryExpression queryContact = new QueryExpression("nav_communication");
            queryContact.ColumnSet = new ColumnSet("nav_phone", "nav_email", "nav_contactid", "nav_type", "nav_main", "nav_name");
            queryContact.NoLock=true;
            queryContact.TopCount = 20;
            queryContact.Criteria.AddCondition("nav_main", ConditionOperator.Equal, true);

            var linkContact = queryContact.AddLink("contact", "nav_contactid", "contactid");
            linkContact.EntityAlias = "c";
            linkContact.Columns = new ColumnSet("fullname", "telephone1", "emailaddress1", "contactid");
            var resultContact = service.RetrieveMultiple(queryContact);
            foreach(var entity in resultContact.Entities)
            {
                Entity contactToUpdate = new Entity("contact", "contactid", entity.GetAttributeValue<AliasedValue>("c.contactid").Value);
                if (entity.GetAttributeValue<bool>("nav_type"))
                {
                    if (entity.GetAttributeValue<AliasedValue>("c.emailaddress1") == null || entity.GetAttributeValue<AliasedValue>("c.emailaddress1").Value == null || (string)entity.GetAttributeValue<AliasedValue>("c.emailaddress1").Value == "")
                    {
                        AddContatInfo(entity, service, true);
                    }
                }
                else
                {
                    if (entity.GetAttributeValue<AliasedValue>("c.telephone1") == null  || entity.GetAttributeValue<AliasedValue>("c.telephone1").Value == null || (string)entity.GetAttributeValue<AliasedValue>("c.telephone1").Value == "")
                    {
                        AddContatInfo(entity, service, false);
                    }
                }              
            }

            //Создаём "средства связи" по данным из контакта
            QueryExpression queryCommunication = new QueryExpression("contact");
            queryCommunication.ColumnSet = new ColumnSet("fullname", "telephone1", "emailaddress1", "contactid");
            queryCommunication.NoLock = true;

            var linkCommunication = queryCommunication.AddLink("nav_communication", "contactid", "nav_contactid", JoinOperator.LeftOuter);
            linkCommunication.EntityAlias = "c";
            linkCommunication.Columns = new ColumnSet("nav_contactid", "nav_phone", "nav_email", "nav_communicationid");
            var resultCommunication = service.RetrieveMultiple(queryCommunication);
            foreach (var entity in resultCommunication.Entities)
            {
                if(entity.GetAttributeValue<AliasedValue>("c.nav_contactid") == null)
                {
                    if(entity.GetAttributeValue<string>("telephone1") != null)
                    {
                        AddCommunication(entity, service, false, true);

                        if (entity.GetAttributeValue<string>("emailaddress1") != null)
                        {
                            AddCommunication(entity, service, true, false);
                        }
                    }
                    else if (entity.GetAttributeValue<string>("emailaddress1") != null)
                    {
                        AddCommunication(entity, service, true, true);
                    }
                }                
            }



            Console.Read();
        }
    }
}
