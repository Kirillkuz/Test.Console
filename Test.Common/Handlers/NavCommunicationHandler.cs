using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Test.Entities;

namespace Test.Common.Handlers
{

    public class NavCommunicationHandler : EntityHandler
    {
        public NavCommunicationHandler(IOrganizationService service, ITracingService tracingService) : base(service, tracingService)
        {
        }


        public void CheckMultipleMainCommunications(nav_communication communication)
        {
            BaseRepository<nav_communication> communicationRepo = new BaseRepository<nav_communication>(Service, nav_communication.EntityLogicalName);

            // Checking if all required communication data is present. If not, obtaining it from CRM.
            if (communication.lys_main == null || communication.lys_type == null || communication.lys_contactid == null)
            {
                communication = communicationRepo.Get(communication.Id, new ColumnSet(nav_communication.Fields.lys_main, nav_communication.Fields.lys_type, nav_communication.Fields.lys_contactid));
            }

            TracingService.Trace($"relatedContactId={communication.lys_contactid}, communicationId={communication.Id}, lys_main={communication.lys_main}, lys_type={communication.lys_type}");

            // No need to check non-set objects.
            if (communication.lys_main == null || communication.lys_main == false || communication.lys_type == null || communication.lys_contactid == null)
            {
                return;
            }

            // Getting all other communications related to our contact with their lys_main=true and the same lys_type.
            QueryExpression query = new QueryExpression();
            query.Criteria.AddCondition(nav_communication.Fields.lys_contactid, ConditionOperator.Equal, communication.lys_contactid.Id);
            query.Criteria.AddCondition(nav_communication.Fields.lys_type, ConditionOperator.Equal, communication.lys_type.Value);
            query.Criteria.AddCondition(nav_communication.Fields.lys_main, ConditionOperator.Equal, true);
            query.Criteria.AddCondition(nav_communication.Fields.nav_communicationId, ConditionOperator.NotEqual, communication.Id);
            query.ColumnSet = new ColumnSet(false);

            query.ColumnSet = new ColumnSet(false);

            EntityCollection ec = communicationRepo.GetMultiple(query);

            TracingService.Trace($"Retrieved nav_communications. ec={ec}, ec.Entities={ec.Entities}, ec.Entities.Count={ec.Entities.Count}");

            if (ec.Entities.Count > 0)
            {
                // Another main communication with the same type is already present.
                throw new EntityHandlerException("Основное средство связи с заданным типом уже существует для связанного контакта.");
            }
        }
    }
}
