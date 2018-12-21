﻿namespace Stratis.Bitcoin.Features.AzureIndexer
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;
    using NBitcoin;
    using Stratis.SmartContracts.CLR;

    public class SmartContactEntry
    {
        public class Entity : IIndexed
        {
            public Entity(TransactionEntry.Entity transactionEntity)
            {
                this.transactionEntity = transactionEntity;
                this.ContractTxData = this.transactionEntity.ContractTxData;
                this.ContractCode = this.transactionEntity.ContractCode;
                this.ContractByteCode = this.transactionEntity.ContractByteCode;
                this.Child = new SmartContactDetailsEntry.Entity(this);
            }

            private readonly TransactionEntry.Entity transactionEntity;

            private string _partitionKey;

            private string _rowKey;

            public SmartContactDetailsEntry.Entity Child { get; set; }

            public string PartitionKey
            {
                get
                {
                    if (this._partitionKey == null && this.TxId != null)
                    {
                        this._partitionKey = this.ContractTxData.ContractAddress.ToString();
                    }

                    return this._partitionKey;
                }
            }

            public string RowKey
            {
                get
                {
                    if (this._rowKey == null && this.TxId != null)
                    {
                        this._rowKey = this.TxId.ToString();
                    }

                    return this._rowKey;
                }
            }

            public DateTimeOffset Timestamp { get; set; }

            public uint256 TxId { get; set; }

            public ContractTxData ContractTxData { get; set; }

            public byte[] ContractByteCode { get; set; }

            public string ContractCode { get; set; }

            public ITableEntity CreateTableEntity()
            {
                return this.CreateTableEntity(null);
            }

            public DynamicTableEntity CreateTableEntity(Network network = null)
            {
                var entity = new DynamicTableEntity
                {
                    ETag = "*", PartitionKey = this.PartitionKey, RowKey = this.RowKey
                };
                entity.Properties.AddOrReplace("GasPrice", new EntityProperty(this.ContractTxData.GasPrice));
                entity.Properties.AddOrReplace("MethodName", new EntityProperty(this.ContractTxData.MethodName));
                entity.Properties.AddOrReplace("OpCode", new EntityProperty(this.ContractTxData.OpCodeType)); // TODO Convert to proper string name

                return entity;
            }

            public ITableEntity GetChildTableEntity()
            {
                return this.Child.CreateTableEntity();
            }

            public IIndexed GetChild()
            {
                return this.Child;
            }
        }
    }
}