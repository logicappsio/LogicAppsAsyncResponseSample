[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

# Logic Apps Async Response Sample

This sample shows how to create an HTTP Async Response pattern that works for Azure Logic Apps. 
For logic details, see the AsyncController. By default, the Logic Apps engine times out 
after 1-2 minutes for an open HTTP Request. When you set up this async pattern, 
you can make the Logic Apps engine wait for a task that takes longer to finish 
(as long as you have data stored for your flow, which is up to 1 year for Premium plans).

## How the sample works

When you get the initial request to start work, start a thread with the long-running task, 
and immediatly return an HTTP Response "202 Accepted" status with a location header. 
The location header points to the URL where the Logic Apps engine can check status for the long-running job. 

By default, the engine checks every 20 seconds, but you can also add a "Retry-after" header 
that specifies the number of seconds until the next poll. After the given time (20 seconds), 
the engine polls the URL on the location header. If the long-running job is still working, 
you should return another "202 Accepted" status with a location header. 
If the job has finished, you should return a "200 OK" status, along with any relevant data. 
The Logic Apps engine uses this data to continue the workflow.
