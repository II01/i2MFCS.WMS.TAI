﻿using i2MFCS.WMS.Database.DTO;
using i2MFCS.WMS.Core.DataExchange;
using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.WCF
{
    [ServiceContract]
    public interface IWMSToUI
    {
        [OperationContract]
        IEnumerable<DTOCommand> GetCommands();

        [OperationContract]
        void CancelOrder(DTOOrder order);

        [OperationContract]
        void CancelCommand(DTOCommand cmd);

        [OperationContract]
        void BlockLocations(string locStartsWith, bool block, int reason);

        [OperationContract]
        void BlockTU(int TUID, bool block, int reason);

        [OperationContract]
        void UpdatePlace(List<PlaceDiff> diffs, string user);

        [OperationContract]
        int SuggestTUID(List<string> tuids);

        [OperationContract]
        void CommandStatusChanged(int cmdId, int status);

        [OperationContract]
        void AddTUs(List<TU> tus);

        [OperationContract]
        void DeleteTU(TU tu);

        [OperationContract]
        void BoxEntry(string box);

        [OperationContract]
        void StoreTUID(int tuid);

        [OperationContract]
        void UpdateERPCommandStatus(int erpid, CommandERP.CommandERPStatus status);
    }
}
