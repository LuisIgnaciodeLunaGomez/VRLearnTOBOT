using System;
using NUnit.Framework;
using UnityEngine;

namespace UBlockly.Test
{
    public class ConnectionTest
    {
        private Workspace mWorkspace;
        private ConnectionModel mInput;
        private ConnectionModel mOutput;
        private ConnectionModel mPrevious;
        private ConnectionModel mNext;

        void Setup()
        {
            mWorkspace = new Workspace();

            Func<BlockModel> createBlock = () =>
            {
                BlockModel block = new BlockModel();
                block.Workspace = mWorkspace;
                return block;
            };

            mInput = new ConnectionModel(createBlock(), Define.EConnection.InputValue);
            mOutput = new ConnectionModel(createBlock(), Define.EConnection.OutputValue);
            mPrevious = new ConnectionModel(createBlock(), Define.EConnection.PrevStatement);
            mNext = new ConnectionModel(createBlock(), Define.EConnection.NextStatement);
        }

        void TearDown()
        {
            mInput = null;
            mOutput = null;
            mPrevious = null;
            mNext = null;
            mWorkspace = null;
        }

        private Func<bool> mIsMovableFn = () => true;

        private ConnectionModel CreateConnection(BlockModel sourceBlock, Vector2<int> location, Define.EConnection type)
        {
            return new ConnectionModel(sourceBlock, type)
            {
                Location = location
            };
        }

        private BlockModel MakeSourceBlock()
        {
            return new BlockModel()
            {
                Workspace = mWorkspace,
                Movable = true,
                IsShadow = false
            };
        }

        [Test]
        public void TestCanConnectWithReason_TargetNull()
        {
            Setup();
            Assert.AreEqual(ConnectionModel.REASON_TARGET_NULL, mInput.CanConnectWithReason(null));
            TearDown();
        }

        [Test]
        public void TestCanConnectWithReason_Disconnect()
        {
            Setup();

            var tempConnection = new ConnectionModel(new BlockModel() {Workspace = mWorkspace}, Define.EConnection.OutputValue);
            ConnectionModel.ConnectReciprocally(mInput, tempConnection);
            Assert.AreEqual(ConnectionModel.CAN_CONNECT, mInput.CanConnectWithReason(mOutput));
            
            TearDown();
        }

        [Test]
        public void TestCanConnnectWithReason_DifferentWorkspace()
        {
            Setup();

            mInput = new ConnectionModel(new BlockModel() {Workspace = new Workspace()}, Define.EConnection.InputValue);
            Assert.AreEqual(ConnectionModel.REASON_DIFFERENT_WORKSPACES, mInput.CanConnectWithReason(mOutput));
            
            TearDown();
        }

        [Test]
        public void TestCanConnectWithReason_Self()
        {
            Setup();
            Assert.AreEqual(ConnectionModel.REASON_SELF_CONNECTION, mInput.CanConnectWithReason(mInput));
            TearDown();
        }

        [Test]
        public void TestCanConnectWithReason_Type()
        {
            Setup();

            Assert.AreEqual(ConnectionModel.REASON_WRONG_TYPE, mInput.CanConnectWithReason(mPrevious));
            Assert.AreEqual(ConnectionModel.REASON_WRONG_TYPE, mInput.CanConnectWithReason(mNext));
            
            Assert.AreEqual(ConnectionModel.REASON_WRONG_TYPE, mOutput.CanConnectWithReason(mPrevious));
            Assert.AreEqual(ConnectionModel.REASON_WRONG_TYPE, mOutput.CanConnectWithReason(mNext));
            
            Assert.AreEqual(ConnectionModel.REASON_WRONG_TYPE, mPrevious.CanConnectWithReason(mInput));
            Assert.AreEqual(ConnectionModel.REASON_WRONG_TYPE, mPrevious.CanConnectWithReason(mOutput));
            
            Assert.AreEqual(ConnectionModel.REASON_WRONG_TYPE, mNext.CanConnectWithReason(mInput));
            Assert.AreEqual(ConnectionModel.REASON_WRONG_TYPE, mNext.CanConnectWithReason(mOutput));
            
            TearDown();
        }

        [Test]
        public void TestCanConnectWithReason_CanConnect()
        {
            Setup();
            
            Assert.AreEqual(ConnectionModel.CAN_CONNECT, mPrevious.CanConnectWithReason(mNext));
            Assert.AreEqual(ConnectionModel.CAN_CONNECT, mNext.CanConnectWithReason(mPrevious));
            Assert.AreEqual(ConnectionModel.CAN_CONNECT, mInput.CanConnectWithReason(mOutput));
            Assert.AreEqual(ConnectionModel.CAN_CONNECT, mOutput.CanConnectWithReason(mInput));
            
            TearDown();
        }

        [Test]
        public void TestCheckConnection_Self()
        {
            Setup();
            //mInput = new Connection(new Block() {Type = "test block"}, Define.EConnection.InputValue);
            try
            {
                mInput.CheckConnection(mInput);
                Assert.Fail();
            }
            catch (Exception e)
            {
                //expected
            }
            
            TearDown();
        }

        [Test]
        public void TestCheckConnection_TypeInputPrev()
        {
            Setup();

            try
            {
                mInput.CheckConnection(mPrevious);
            }
            catch (Exception e)
            {
                //expected
            }
            
            TearDown();
        }

        [Test]
        public void TestCheckConnection_TypeOutputPrev()
        {
            Setup();
            try
            {
                mOutput.CheckConnection(mPrevious);
            }
            catch (Exception e)
            {
                //expected
            }
            TearDown();
        }

        [Test]
        public void TestCheckConnection_TypePrevInput()
        {
            Setup();

            try
            {
                mPrevious.CheckConnection(mInput);
            }
            catch (Exception e)
            {
                //expected
            }
            TearDown();
        }

        [Test]
        public void TestCheckConnection_TypePrevOutput()
        {
            Setup();

            try
            {
                mPrevious.CheckConnection(mOutput);
            }
            catch (Exception e)
            {
                //expected
            }
            TearDown();
        }

        [Test]
        public void TestCheckConnection_TypeNextInput()
        {
            Setup();

            try
            {
                mNext.CheckConnection(mInput);
            }
            catch (Exception e)
            {
                //expected
            }
            TearDown();
        }
        
        [Test]
        public void TestCheckConnection_TypeNextOutput()
        {
            Setup();

            try
            {
                mNext.CheckConnection(mOutput);
            }
            catch (Exception e)
            {
                //expected
            }
            TearDown();
        }

        [Test]
        public void TestIsConnectionAllowed_Distance()
        {
            Setup();
            
            BlockModel sourceBlock = MakeSourceBlock();
            ConnectionModel one = CreateConnection(sourceBlock, new Vector2<int>(5, 10), Define.EConnection.InputValue);

            sourceBlock = MakeSourceBlock();
            ConnectionModel two = CreateConnection(sourceBlock, new Vector2<int>(10, 15), Define.EConnection.OutputValue);

            Assert.True(one.IsConnectionAllowed(two, 20));

            two.Location = new Vector2<int>(100, 100);
            Assert.False(one.IsConnectionAllowed(two, 20));
            
            TearDown();
        }
        
        [Test]
        public void TestIsConnectionAllowed_Unrendered()
        {
            Setup();
            
            BlockModel sourceBlock = MakeSourceBlock();
            ConnectionModel one = CreateConnection(sourceBlock, new Vector2<int>(5, 10), Define.EConnection.InputValue);
            
            sourceBlock = MakeSourceBlock();
            ConnectionModel two = CreateConnection(sourceBlock, new Vector2<int>(0, 0), Define.EConnection.OutputValue);
            
            Assert.True(one.IsConnectionAllowed(two));
            
            sourceBlock = MakeSourceBlock();
            ConnectionModel three = CreateConnection(sourceBlock, new Vector2<int>(0, 0), Define.EConnection.InputValue);

            ConnectionModel.ConnectReciprocally(two, three);
            Assert.False(one.IsConnectionAllowed(two));

            two = CreateConnection(one.SourceBlock, new Vector2<int>(0, 0), Define.EConnection.OutputValue);
            Assert.False(one.IsConnectionAllowed(two));
            
            TearDown();
        }

        [Test]
        public void TestIsConnectionAllowed_NoNext()
        {
            Setup();
            
            BlockModel sourceBlock = MakeSourceBlock();
            ConnectionModel one = CreateConnection(sourceBlock, new Vector2<int>(0, 0), Define.EConnection.NextStatement);
            one.SourceBlock.NextConnection = one;
            
            sourceBlock = MakeSourceBlock();
            ConnectionModel two = CreateConnection(sourceBlock, new Vector2<int>(0, 0), Define.EConnection.PrevStatement);
            
            Assert.True(two.IsConnectionAllowed(one));
            
            sourceBlock = MakeSourceBlock();
            ConnectionModel three = CreateConnection(sourceBlock, new Vector2<int>(0, 0), Define.EConnection.PrevStatement);
            three.SourceBlock.PreviousConnection = three;
            ConnectionModel.ConnectReciprocally(one, three);
            
            Assert.True(two.IsConnectionAllowed(one));
            
            TearDown();
        }

        [Test]
        public void TestCheckConnectionOkay()
        {
            Setup();
            
            mPrevious.CheckConnection(mNext);
            mNext.CheckConnection(mPrevious);
            mInput.CheckConnection(mOutput);
            mOutput.CheckConnection(mInput);
            
            TearDown();
        }
    }
}
