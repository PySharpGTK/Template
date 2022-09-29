using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using System.Linq.Expressions;

namespace Template.libs;


public class Form
{
    private List<dynamic> events;
    public Dictionary<string, object> bindings;
    
}

// move to partial class?
public class Button
{
    private SocketStrapper strapper;
    public string id;
    
    public Button(SocketStrapper _strapper, string _id)
    {
        strapper = _strapper;
        id = _id;
    }

    public bool Update(string data)
    {
        strapper.Send(JsonConvert.SerializeObject(
            new Dictionary<string, string>()
            {
                { "id", "data_out" },
                { "data", data }
            }
        ));
        return true;
    }
}

public class Label
{
    private SocketStrapper strapper;
    public string id;
    
    public Label(SocketStrapper _strapper, string _id)
    {
        strapper = _strapper;
        id = _id;
    }

    public bool Update(string data)
    {
        strapper.Send(JsonConvert.SerializeObject(
            new Dictionary<string, string>()
            {
                { "id", "data_out" },
                { "data", data }
            }
        ));
        return true;
    }
}

public class SocketStrapper
{
    Thread mThread;
    public string connectionIP = "0.0.0.0";
    public int connectionPort = 5757;
    IPAddress localAdd;
    TcpListener listener;

    TcpClient client;
    // Something to store recieved data?

    public MethodInfo[] methods;
    public Type targetType;
    private List<string> fieldNames;
    private bool active;
    private Dictionary<string, dynamic> eventDict = new Dictionary<string, dynamic>();
    private Dictionary<string, dynamic> bindingsDict = new Dictionary<string, dynamic>();

    public void Start()
    {
        ThreadStart ts = new ThreadStart(GetInfo);
        mThread = new Thread(ts);
        mThread.Start();
    }

    public void ConnectEvents(Type _targetType, Dictionary<string, dynamic> events)
    {
        methods = _targetType.GetMethods();
        Dictionary<string, System.Reflection.MethodInfo> methodDict = methods.ToDictionary(f => f.Name, f => f);
        ;
        Console.WriteLine($"Loaded {methods.Length} methods");
        // Console.WriteLine(methodDict);
        methodDict.Select(i => $"{i.Key}: {i.Value}").ToList().ForEach(Console.WriteLine);

        // Update our target
        targetType = _targetType;

        fieldNames = _targetType.GetFields()
            .Select(field => field.Name)
            .ToList();


        // Now we want to map the events to what shit to run on them
        foreach (var keyValuePair in events)
        {
            eventDict[keyValuePair.Key] = methodDict[keyValuePair.Value];
        }

        Console.WriteLine("-----");
        eventDict.Select(i => $"{i.Key}: {i.Value}").ToList().ForEach(Console.WriteLine);
    }

    void GetInfo()
    {
        localAdd = IPAddress.Parse(connectionIP);
        listener = new TcpListener(IPAddress.Any, connectionPort);
        listener.Start();

        client = listener.AcceptTcpClient();

        active = true;
        while (active)
        {
            SendAndReceiveData();
        }

        listener.Stop();
    }

    void SendAndReceiveData()
    {
        NetworkStream nwStream = client.GetStream();
        byte[] buffer = new byte[client.ReceiveBufferSize];

        //---receiving Data from the Host----
        int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize); //Getting data in Bytes from Python
        string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead); //Converting byte data to string

        if (dataReceived != null)
        {
            //---Using received data---
            Console.WriteLine(dataReceived);
            var dic = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(dataReceived);
            if (dic.ContainsKey("type"))
            {
                switch (dic["type"])
                {
                    case "Button":
                        if (dic.ContainsKey("id")
                            &
                            eventDict.ContainsKey(dic["id"])
                           )
                        {
                            // eventDict[dic["id"]]();
                            Console.WriteLine($"{dic["id"]} {eventDict[dic["id"]]}");
                            // Delegate d = eventDict[dic["id"]].Method;
                            var eventDelegate = CreateDelegate(eventDict[dic["id"]]);
                            eventDelegate.DynamicInvoke(bindingsDict);
                        }

                        break;
                    case "Entry":
                        if (
                            dic.ContainsKey("id") &
                            bindingsDict.ContainsKey(dic["id"])
                        )
                        {
                            // We want to update the attr of our target
                            bindingsDict[dic["id"]] = dic["text"];
                        }

                        break;
                    case "ctx":
                        Dictionary<string, dynamic> subBindings = dic["data"].ToObject<Dictionary<string, dynamic>>();
                        bindingsDict = subBindings;
                        // Process labels and etc
                        // We want each type to have it's own structure and etc
                        foreach (var keyValuePair in bindingsDict)
                        {
                            bindingsDict[keyValuePair.Key] = (keyValuePair.Value switch
                            {
                                "Button" => new Button(this, keyValuePair.Key),
                                "Label" => new Label(this, keyValuePair.Key),
                                _ => null
                            })!;
                        }

                        bindingsDict.Select(i => $"{i.Key}: {i.Value}").ToList().ForEach(Console.WriteLine);
                        break;
                }
            }

            //---Sending Data to Host----
            // Bounce reply
            byte[] myWriteBuffer = Encoding.ASCII.GetBytes("");
            nwStream.Write(myWriteBuffer, 0, myWriteBuffer.Length);
            Console.WriteLine("Sent?");
        }
    }

    public void Send(string data)
    {
        NetworkStream nwStream = client.GetStream();
        byte[] myWriteBuffer = Encoding.ASCII.GetBytes(data);
        nwStream.Write(myWriteBuffer, 0, myWriteBuffer.Length);
        Console.WriteLine("Sent custom?");
    }

    // Below is shamelessly stolen ;P
    static Delegate CreateDelegate(MethodInfo method)
    {
        if (method == null)
        {
            throw new ArgumentNullException("method");
        }

        if (!method.IsStatic)
        {
            throw new ArgumentException("The provided method must be static.", "method");
        }

        if (method.IsGenericMethod)
        {
            throw new ArgumentException("The provided method must not be generic.", "method");
        }

        return method.CreateDelegate(Expression.GetDelegateType(
            (from parameter in method.GetParameters() select parameter.ParameterType)
            .Concat(new[] { method.ReturnType })
            .ToArray()));
    }
}
