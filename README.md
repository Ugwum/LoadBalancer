# Load Balancer Implementation in C#

This repository contains an implementation of a load balancer in C# that distributes incoming HTTP requests to a group of backend servers based on various load distribution algorithms. This load balancer is designed to improve the availability, scalability, and reliability of your application by evenly distributing the incoming traffic across multiple servers.

## Features

- **Multiple Load Distribution Algorithms:** The load balancer supports several load distribution algorithms, including Round Robin, Weighted Round Robin, Random, Least Connections, Least Response Time, Least Requests, and Consistent Hash.

- **Retry Mechanism:** It uses the Polly library to define a retry policy, allowing the load balancer to automatically retry failed requests, improving the fault tolerance of your system.

- **Health Checks:** The load balancer performs health checks on backend servers before routing requests to them, ensuring that only healthy servers handle incoming traffic.

- **Backend Server Management:** You can easily add or remove backend servers from the load balancer as needed.

## Prerequisites

- .NET Core or .NET 5 (or later) SDK installed on your development machine.
- A basic understanding of load balancing concepts and HTTP requests.

## Getting Started

Follow these steps to get started with the load balancer:

1. **Clone this repository to your local machine:**

   ```bash
   git clone https://github.com/Ugwum/LoadBalancer.git

2. **Open the project in your favorite C# development environment (e.g., Visual Studio, Visual Studio Code).**

3. **Build the solution to ensure all dependencies are resolved.**

4. **Configure your backend servers by modifying the BackendServer objects in the code.**

5. **Run the load balancer application.**

6. **Make HTTP requests to the load balancer, and it will distribute the requests to the backend servers based on the selected load distribution algorithm.**

## Configuration

You can configure various aspects of the load balancer, such as the maximum number of retries, load distribution algorithm, and backend server details, by editing the appsettings.json file.

## Usage
Here's an example of how to use the load balancer in your C# application:

	```bash
	// Create an instance of the load balancer
	ILoadBalancer loadBalancer = new AdvancedLoadBalancer(backendServers, LoadDistributionAlgorithm.RoundRobin, httpClientFactory);

	// Create an HTTP request
	var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/resource");

	// Send the request through the load balancer
	HttpResponseMessage response = await loadBalancer.SendRequestAsync(request);


## Contributing
Contributions are welcome! If you'd like to improve this load balancer or add new features, please fork the repository and submit a pull request. For major changes, please open an issue first to discuss the proposed changes.

## License
This project is licensed under the MIT License - see the LICENSE.md file for details.

## Acknowledgments
Polly - A resilience and transient-fault-handling library for .NET.
Additional Resources
Load Balancing Algorithms - Learn more about load balancing algorithms and strategies.

Polly Documentation - Explore the documentation for Polly, the library used for implementing retry policies.

## Author
Obinna Agim 

### Contact
For any inquiries or feedback, please contact obieziefule@gmail.com.

