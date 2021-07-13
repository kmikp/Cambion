﻿using System;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Whitestone.Cambion.Events;
using Whitestone.Cambion.Interfaces;

namespace Whitestone.Cambion.Transport.RabbitMQ
{
    public class RabbitMqTransport : ITransport
    {
        public ISerializer Serializer { get; set; }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        private readonly RabbitMqConfig _config;
        private IConnection _conn;
        private IModel _channel;

        public RabbitMqTransport(IOptions<RabbitMqConfig> config)
        {
            _config = config.Value;
        }

        public void Start()
        {
            _config.AssertIsValid();

            ConnectionFactory factory = new ConnectionFactory();
            if (_config.Connection.ConnectionString != null)
            {
                factory.Uri = _config.Connection.ConnectionString;
            }
            else
            {
                factory.HostName = _config.Connection.Hostname;
                factory.Port = _config.Connection.Port;
                factory.VirtualHost = _config.Connection.VirtualHost;
                factory.UserName = _config.Connection.Username;
                factory.Password = _config.Connection.Password;
            }

            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();

            // Declare exchange and queue and bind them together
            _channel.ExchangeDeclare(_config.Exchange.Name, _config.Exchange.Type, _config.Exchange.Durable, _config.Exchange.AutoDelete);
            _channel.QueueDeclare(_config.Queue.Name, _config.Queue.Durable, _config.Queue.Exclusive, _config.Queue.AutoDelete);
            _channel.QueueBind(_config.Queue.Name, _config.Exchange.Name, string.Empty);

            // Set up listening for queue messages
            EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
            consumer.Received += QueueDataReceived;
            _channel.BasicConsume(_config.Queue.Name, true, consumer);
        }

        public void Stop()
        {
            _channel.Close();
            _conn.Close();
        }

        public void Publish(byte[] messagebytes)
        {
            if (messagebytes == null)
            {
                throw new ArgumentNullException(nameof(messagebytes));
            }

            lock (_channel)
            {
                _channel.BasicPublish(_config.Exchange.Name, string.Empty, null, messagebytes);
            }
        }

        private void QueueDataReceived(object sender, BasicDeliverEventArgs e)
        {
            ReadOnlyMemory<byte> messageBytes = e.Body;

            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(messageBytes.ToArray()));
        }
    }
}
