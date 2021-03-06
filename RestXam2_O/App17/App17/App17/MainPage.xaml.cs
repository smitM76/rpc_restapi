﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace App17
{
    /*
    creating classes data Meta,Object,RootObject with getter setter method
    this classes varies as per json fields 
    used "http://json2csharp.com/" to auto generate this classes
    which helps us to bind and handles the json responce we going to get from rpc call
    */
    public class Meta
    {
        public int limit { get; set; }
        public object next { get; set; }
        public int offset { get; set; }
        public object previous { get; set; }
        public int total_count { get; set; }
    }

    public class Object
    {
        public string body { get; set; }
        public DateTime created_at { get; set; }
        public int id { get; set; }
        public string resource_uri { get; set; }
        public string title { get; set; }
    }

    public class RootObject
    {
        public Meta meta { get; set; }
        public List<Object> objects { get; set; }
    }
    /*
     client class.
     It's going to expose a method named call which sends an RPC request and blocks until the answer is received
    */
    public class RpcClient
    {
        //initializing read-only variables for later uses
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly EventingBasicConsumer consumer;
        private readonly BlockingCollection<string> respQueue = new BlockingCollection<string>();
        private readonly IBasicProperties props;

        public RpcClient()
        {
            //establishing connection with my rabbitmq message broker
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri("amqp://udpbbklh:Ttvn6qar8lu2aLE-ie3CmvdZ1ReLLg3k@bee.rmq.cloudamqp.com/udpbbklh");

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            replyQueueName = channel.QueueDeclare().QueueName;
            consumer = new EventingBasicConsumer(channel);

            props = channel.CreateBasicProperties();
            /*
             generating a unique CorrelationId number and save it
            */
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var response = Encoding.UTF8.GetString(body);
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    respQueue.Add(response);
                }
            };
        }
        /*
        handling the request  
        crud operation name and user parameters 
        */
         
        public string Call(string message)
        {

            var messageBytes = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(
                exchange: "",
                routingKey: "rpc_queue",
                basicProperties: props,
                body: messageBytes);
  
            channel.BasicConsume(
                consumer: consumer,
                queue: replyQueueName,
                autoAck: true);

            return respQueue.Take(); ;
        }

        public void Close()
        {
            connection.Close();
        }
    }
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
        protected override async void OnAppearing() {
        
            //initialization of instance of rpcClient
            var rpcClient = new RpcClient();
            //setting my request method to get
            string sms = "get/";
            /*
            passing the value to rpcClient.call() this will send the request to the server,
            and wait for response
            */
            var response = rpcClient.Call(sms);

            //handling the response
            //decerializing json string to object
            var post = JsonConvert.DeserializeObject<List<Object>>(response);
            //binding the json object with listview item fields id,title,body
            Post_List.ItemsSource = post;
            //load the data when app starts
            base.OnAppearing();
        }
        //button event for post method
        private async void onAdd(object sender, System.EventArgs e) {
            var rpcClient = new RpcClient();
            //setting the values to be passed to call method
            string sms = "post/" + "title/" + EntTitle.Text + "/" + "body/"+ EntBody.Text + "/";
            //calling the method for post request
            var response = rpcClient.Call(sms);
            await DisplayAlert("alert", "post request sent", "ok");
        }
        private async void onPut(object sender,System.EventArgs e) {
            var rpcClient = new RpcClient();
            //setting the values to be passed to call method
            var sms = "put/" + Convert.ToInt32(EntId.Text) + "/" + "title/" + EntBody.Text + "/" + "body/" + EntBody.Text + "/";
            var response = rpcClient.Call(sms);
            await DisplayAlert("alert", "put request sent", "ok");
        }
        private async void onDelete(object sender,System.EventArgs e) {
            var rpcClient = new RpcClient();
            //setting the values to be passed to call method
            var sms = "delete/" + Convert.ToInt32(EntId.Text) + "/";
            var response = rpcClient.Call(sms);
            await DisplayAlert("alert", "delete sent", "ok");
        }
    }
}
