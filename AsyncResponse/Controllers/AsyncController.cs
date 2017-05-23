using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace AsyncResponse.Controllers
{
    public class AsyncController : ApiController
    {
        // Sample state dictionary to store the state for the working thread
        private static Dictionary<Guid, bool> runningTasks = new Dictionary<Guid, bool>();

        /// <summary>
        /// This method starts the task, creates a new thread to do work, 
        /// and returns an ID that you can pass to the Logic Apps engine for checking job status. 
        /// In a real world scenario, your dictionary could contain the object that you want to return after work is done.
        /// </summary>
        /// <returns>HTTP Response with needed headers</returns>
        [HttpPost]
        [Route("api/startwork")]
        public async Task<HttpResponseMessage> longrunningtask()
        {
            Guid id = Guid.NewGuid();  // Generate tracking ID for checking job status
            runningTasks[id] = false;  // Job not done yet
            new Thread(() => doWork(id)).Start();  // Start the thread to do work, but continue on before the job completes
            HttpResponseMessage responseMessage = Request.CreateResponse(HttpStatusCode.Accepted);   
            responseMessage.Headers.Add("location", String.Format("{0}://{1}/api/status/{2}", Request.RequestUri.Scheme, Request.RequestUri.Host, id));  // The URL where the engine can poll for job status
            responseMessage.Headers.Add("retry-after", "20");  // The number of seconds that the engine should wait before polling again. The default is 20 seconds when not included.
            return responseMessage;
        }

        /// <summary>
        /// This method performs the actual long-running work.
        /// </summary>
        /// <param name="id"></param>
        private void doWork(Guid id)
        {
            Debug.WriteLine("Starting work");
            Task.Delay(120000).Wait(); // Do work for 120 seconds.
            Debug.WriteLine("Work completed");
            runningTasks[id] = true;  // Set flag to true when work is done.
        }

        /// <summary>
        /// This method checks the job's status and is also the location where the location header redirects.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/status/{id}")]
        [Swashbuckle.Swagger.Annotations.SwaggerResponse(HttpStatusCode.BadRequest, "No job exists with the specified ID")]
        [Swashbuckle.Swagger.Annotations.SwaggerResponse(HttpStatusCode.Accepted, "The job is still running")]
        [Swashbuckle.Swagger.Annotations.SwaggerResponse(HttpStatusCode.OK, "The job has completed")]
        public HttpResponseMessage checkStatus([FromUri] Guid id)
        {
            // If the job is done, return "200 OK" status with response payload (data output).
            if(runningTasks.ContainsKey(id) && runningTasks[id])
            {
                runningTasks.Remove(id);
                return Request.CreateResponse(HttpStatusCode.OK, "Can return some data here");
            }
            // If the job is still running, return "202 ACCEPTED" status, the URL where to check again for job status, and the interval for checking status.
            else if(runningTasks.ContainsKey(id))
            {
                HttpResponseMessage responseMessage = Request.CreateResponse(HttpStatusCode.Accepted);
                responseMessage.Headers.Add("location", String.Format("{0}://{1}/api/status/{2}", Request.RequestUri.Scheme, Request.RequestUri.Host, id)); // The URL where the engine can poll for job status
                responseMessage.Headers.Add("retry-after", "20");
                return responseMessage;
            }
            // No job matching the specified ID was found.
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No job exists with the specified ID");
            }
        }
    }
}
