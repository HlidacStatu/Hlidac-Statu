using RabbitMQ.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HlidacStatu.Q.Simple
{
    public class Response<T>
    {
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public T Value { get; set; }
        public ulong? ResponseId { get; set; } = null;
    }

    public class Queue<T>
        : IDisposable

    {
        public string QueueName { get; }

        private readonly ConnectionFactory factory;
        private readonly IConnection connection;
        private readonly IModel channel;
        private bool disposedValue;

        public Queue(string queueName, string connectionString)
        {
            QueueName = queueName;
            //parse connectionString
            //host=ip;username=usr;password=pswd
            string[] cnnstrParts = connectionString.Split(';');
            string host = cnnstrParts.Where(m => m.StartsWith("host=")).Select(m => m.Split('=')).FirstOrDefault()?[1];
            string usrn = cnnstrParts.Where(m => m.StartsWith("username=")).Select(m => m.Split('=')).FirstOrDefault()?[1];
            string pswd = cnnstrParts.Where(m => m.StartsWith("password=")).Select(m => m.Split('=')).FirstOrDefault()?[1];

            factory = new ConnectionFactory() { HostName = host, UserName = usrn, Password = pswd };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        }

        public long ConsumerCount()
        {
            try
            {
                return channel.ConsumerCount(this.QueueName);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public long MessageCount()
        {
            try
            {
                return channel.MessageCount(this.QueueName);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public void Send(T message)
        {
            var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(message));
            channel.BasicPublish(exchange: "", routingKey: QueueName, basicProperties: null, body: body);
        }
        public void Send(IEnumerable<T> messages)
        {
            var batch = channel.CreateBasicPublishBatch();

            foreach (var m in messages)
            {
                var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(m));
                batch.Add(exchange: "", routingKey: QueueName, false, null, body);
            }
            batch.Publish();
        }

        public T GetAndAck()
        {
            return GetAndAck(out _);
        }
        public T GetAndAck(out ulong? responseId)
        {
            responseId = null;
            Response<T> res = Get(true);
            if (res == null)
                return default(T);

            responseId = res.ResponseId;

            return res.Value;

        }
        public Response<T> Get(bool autoack = false)
        {

            //var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(message));
            var res = channel.BasicGet(QueueName, autoack);
            if (res == null)
                return null;

            var body = Encoding.UTF8.GetString(res.Body.ToArray());
            return new Response<T>()
            {
                Value = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(body),
                ResponseId = res.DeliveryTag
            };
        }

        public void AckMessage(ulong responseId)
        {
            channel.BasicAck(responseId, false);
        }
        public void RejectMessage(ulong responseId)
        {
            channel.BasicReject(responseId, true);
        }
        public void RejectMessageOnTheEnd(ulong responseId, T value)
        {
            AckMessage(responseId);
            Send(value);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    channel.Dispose();
                    connection.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Queue()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Static

        #endregion
    }
}
