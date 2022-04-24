using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Abraham.Threading
{
    /// <summary>
    /// Implements a producer/consumer-Pattern with a consumer-Thread
    /// </summary>
    /// <typeparam name="T">Type of the data elements</typeparam>
    public class ProducerConsumerDecuplingThread<T>
    {
        #region ------------- Properties ----------------------------------------------------------

        public delegate void OnProducerInput_Handler (T message);
        public OnProducerInput_Handler OnProducerInput
        {
            get { return _OnProducerInput; }
            set { if (value != null) _OnProducerInput = value; else _OnProducerInput = OnProducerInput_Nullobject; }
        }
        private void OnProducerInput_Nullobject (T message) { }
        private OnProducerInput_Handler _OnProducerInput;



        public delegate void OnConsumerOutput_Handler (T message);
        public OnConsumerOutput_Handler OnConsumerOutput 
        {
            get { return _OnConsumerOutput; }
            set { if (value != null) _OnConsumerOutput = value; else _OnConsumerOutput = OnConsumerOutput_Nullobject; }
        }
        private void OnConsumerOutput_Nullobject (T message) { }
        private OnConsumerOutput_Handler _OnConsumerOutput;



        public delegate void OnMessageQueueEmpty_Handler ();
        public OnMessageQueueEmpty_Handler OnMessageQueueEmpty 
        {
            get { return _OnMessageQueueEmpty; }
            set { if (value != null) _OnMessageQueueEmpty = value; else _OnMessageQueueEmpty = OnMessageQueueEmpty_Nullobject; }
        }
        private void OnMessageQueueEmpty_Nullobject () { }
        private OnMessageQueueEmpty_Handler _OnMessageQueueEmpty;

        public int Consumer_Idle_Delay { get; set; }
        #endregion



        #region ------------- Fields --------------------------------------------------------------

        private BlockingCollection<T> _MessageQueue;

        private Object _LockObject = new Object();

        private ThreadExtensions _ConsumerThread;
		private bool _ThreadIsClosing;

		#endregion



		#region ------------- Init ----------------------------------------------------------------

		public ProducerConsumerDecuplingThread()
        {
            Consumer_Idle_Delay = 1000;
            OnProducerInput     = null;
            OnConsumerOutput    = null;
            OnMessageQueueEmpty = null;
            _MessageQueue       = new BlockingCollection<T>();
            StartTimer();
        }

        #endregion



        #region ------------- Methods -------------------------------------------------------------

        public void Stop()
        {
			_ThreadIsClosing = true;
			_MessageQueue.Add(default(T));

            if (_ConsumerThread != null)
                _ConsumerThread.SendStopSignalAndWait();
        }
        
        public void Put(T message)
        {
            lock (_LockObject)
            {
                _MessageQueue.Add(message);
            }
            _OnProducerInput(message);
        }

        #endregion



        #region ------------- Implementation ------------------------------------------------------

        private void StartTimer()
        {
            _ConsumerThread = new ThreadExtensions(ConsumerThreadProc, "ProducerConsumerDecuplingThread");
            _ConsumerThread.thread.Start();
        }

        /// <summary>
        /// Schreibt nach einer Sekunde Wartezeit alles, was sich 
        /// in der Warteschlange angesammelt hat, in die Datei.
        /// </summary>
        private void ConsumerThreadProc()
        {
            do
			{
				try
				{
					if (_MessageQueue.Count > 0)
					{
						while (_MessageQueue.Count > 0 && !_ThreadIsClosing)
						{
							var watch = new Stopwatch();
							watch.Start();

							var Message = _MessageQueue.Take();
							_OnConsumerOutput(Message);

							//System.Diagnostics.Debug.WriteLine($"--------------------------- {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} _ProducerConsumer thread is taking, elements in queue now {_MessageQueue.Count} processing took  {watch.ElapsedMilliseconds} ms");
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"FileWriterThreadProc: {ex.ToString()}");
				}

				if (_ThreadIsClosing)
					break;

				_OnMessageQueueEmpty();
				Wait_if_no_message_is_present();
			}
			while (_ConsumerThread.Run);
        }

		private void Wait_if_no_message_is_present()
		{
			if (Consumer_Idle_Delay != 0)
			{
				if (Consumer_Idle_Delay <= 100)
					Thread.Sleep(Consumer_Idle_Delay);
				else
				{
					int Delay = Consumer_Idle_Delay;
					do
					{
						Thread.Sleep(100);
						Delay -= 100;
					}
					while (Delay > 0 && _MessageQueue.Count == 0);
				}

			}
		}

		#endregion
	}
}
