protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await HandleFirstConnection();

            try
            {
                IJetStreamManagement jsm = _connection!.CreateJetStreamManagementContext();
                IJetStream js = _connection.CreateJetStreamContext();

                if (!jsm.GetStreamNames().Contains(_options.Value.NatsStreamName))
                {
                    StreamConfiguration streamConfig = StreamConfiguration.Builder()
                        .WithName(_options.Value.NatsStreamName).WithSubjects(_subjects).WithMaxAge(604800000)
                        .WithStorageType(StorageType.File).Build();
                    StreamInfo streamInfo = jsm.AddStream(streamConfig);
                    _logger.LogInformation("{StreamInfo}", streamInfo);
                }

                if (!jsm.GetConsumerNames(_options.Value.NatsStreamName).Contains(NatsConsumerName))
                {
                    ConsumerConfiguration cc = ConsumerConfiguration.Builder()
                        .WithDurable(NatsConsumerName).WithDeliverSubject("carrier-delivery")
                        .WithFilterSubject(CarrierOrderChangedTopic).Build();
                    ConsumerInfo consumerInfo = _connection!.CreateJetStreamManagementContext()
                        .AddOrUpdateConsumer(_options.Value.NatsStreamName, cc);
                    _logger.LogInformation("{ConsumerInfo}", consumerInfo);
                }

                PushSubscribeOptions pso = PushSubscribeOptions.Builder()
                    .WithDurable(NatsConsumerName).WithBind(true)
                    .WithStream(_options.Value.NatsStreamName).Build();

                js.PushSubscribeAsync(CarrierOrderChangedTopic, HandleOrderEventHandler!, false, pso);
            }
            catch (NATSException e)
            {
                _logger.LogError("NATS exception running\n Source: {Source}\n Error: {Error}", 
                    e.Source, e.Message);
            }

            while (_connection!.State == ConnState.CONNECTED)
            {
                await Task.Delay(5000, stoppingToken);
            }

            _connection.Close();
        }
    }