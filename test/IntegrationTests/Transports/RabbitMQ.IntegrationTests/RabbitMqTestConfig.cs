﻿namespace Whitestone.Cambion.IntegrationTests.Transports.RabbitMQ
{
    internal class RabbitMqTestConfig
    {
        public string ConnectionString { get; set; }
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
    }
}
