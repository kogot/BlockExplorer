﻿namespace Stratis.Bitcoin.Features.AzureIndexer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using NBitcoin;
    using Stratis.Bitcoin.Features.AzureIndexer.IndexTasks;

    public class BlockInfo
    {
        public int Height { get; set; }

        public uint256 BlockId { get; set; }

        public Block Block { get; set; }
    }

    public class BlockFetcher : IEnumerable<BlockInfo>
    {
        private readonly Checkpoint _Checkpoint;

        public Checkpoint Checkpoint
        {
            get
            {
                return _Checkpoint;
            }
        }

        private readonly IBlocksRepository _BlocksRepository;

        public IBlocksRepository BlocksRepository
        {
            get
            {
                return _BlocksRepository;
            }
        }

        private readonly ILoggerFactory loggerFactory;

        private readonly ILogger logger;

        private readonly ChainBase _BlockHeaders;

        public ChainBase BlockHeaders
        {
            get
            {
                return _BlockHeaders;
            }
        }

        private void InitDefault()
        {
            NeedSaveInterval = TimeSpan.FromMinutes(15);
            ToHeight = int.MaxValue;
        }

        public BlockFetcher(Checkpoint checkpoint, IBlocksRepository blocksRepository, ChainBase chain, ChainedHeader lastProcessed, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this.logger = this.loggerFactory.CreateLogger(GetType().FullName);

            if (blocksRepository == null)
            {
                throw new ArgumentNullException("blocksRepository");
            }

            if (chain == null)
            {
                throw new ArgumentNullException("blockHeaders");
            }

            if (checkpoint == null)
            {
                throw new ArgumentNullException("checkpoint");
            }

            _BlockHeaders = chain;
            _BlocksRepository = blocksRepository;
            _Checkpoint = checkpoint;
            _LastProcessed = lastProcessed;

            InitDefault();
        }

        public TimeSpan NeedSaveInterval { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public ChainedHeader _LastProcessed { get; private set; }

        public IEnumerator<BlockInfo> GetEnumerator()
        {
            Queue<DateTime> lastLogs = new Queue<DateTime>();
            Queue<int> lastHeights = new Queue<int>();

            ChainedHeader fork = this._BlockHeaders.FindFork(this._Checkpoint.BlockLocator);
            IEnumerable<ChainedHeader> headers = this._BlockHeaders.EnumerateAfter(fork);
            headers = headers.Where(h => h.Height <= ToHeight);
            ChainedHeader first = headers.FirstOrDefault();
            if (first == null)
            {
                yield break;
            }

            var height = first.Height;
            if (first.Height == 1)
            {
                headers = new[] { fork }.Concat(headers);
                height = 0;
            }

            foreach (Block block in _BlocksRepository.GetBlocks(headers.Select(b => b.HashBlock), CancellationToken))
            {
                ChainedHeader header = _BlockHeaders.GetBlock(height);

                if (block == null)
                {
                    Block storeTip = _BlocksRepository.GetStoreTip();
                    if (storeTip != null)
                    {
                        // Store is caught up with Chain but the block is missing from the store.
                        if (header.Header.BlockTime <= storeTip.Header.BlockTime)
                        {
                            throw new InvalidOperationException($"Chained block not found in store (height = { height }). Re-create the block store.");
                        }
                    }

                    // Allow Store to catch up with Chain.
                    break;
                }

                _LastProcessed = header;
                yield return new BlockInfo()
                {
                    Block = block,
                    BlockId = header.HashBlock,
                    Height = header.Height
                };

                IndexerTrace.Processed(height, Math.Min(ToHeight, _BlockHeaders.Tip.Height), lastLogs, lastHeights);
                height++;
            }
        }

        internal void SkipToEnd()
        {
            var height = Math.Min(ToHeight, _BlockHeaders.Tip.Height);
            _LastProcessed = _BlockHeaders.GetBlock(height);
            IndexerTrace.Information("Skipped to the end at height " + height);
        }

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private DateTime _LastSaved = DateTime.UtcNow;

        public bool NeedSave
        {
            get
            {
                return (DateTime.UtcNow - _LastSaved) > NeedSaveInterval;
            }
        }

        public void SaveCheckpoint()
        {
            this.logger.LogTrace("()");

            if (_LastProcessed != null)
            {
                this.logger.LogTrace("Saving checkpoints");

                _Checkpoint.SaveProgress(_LastProcessed);
                IndexerTrace.CheckpointSaved(_LastProcessed, _Checkpoint.CheckpointName);
            }

            _LastSaved = DateTime.UtcNow;

            this.logger.LogTrace("(-)");
        }

        public int FromHeight { get; set; }

        public int ToHeight { get; set; }
    }
}
