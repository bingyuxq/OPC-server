using Opc.Ua.Server;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace OPC_server
{

    /// <summary>
    /// Helpers to find node managers implemented in this library.
    /// </summary>

    public static class Utils
    {
        /// <summary>
        /// The property with available node manager factories.
        /// </summary>
        public static ReadOnlyList<INodeManagerFactory> NodeManagerFactories
        {
            get
            {
                if (m_nodeManagerFactories == null)
                {
                    m_nodeManagerFactories = GetNodeManagerFactories();
                }
                return new ReadOnlyList<INodeManagerFactory>(m_nodeManagerFactories);
            }
        }
        /// <summary>
        /// Enumerates all node manager factories.
        /// </summary>
        /// <returns></returns>
        private static IList<INodeManagerFactory> GetNodeManagerFactories()
        {
            var assembly = typeof(Utils).Assembly;
            var nodeManagerFactories = assembly.GetExportedTypes().Select(type => IsINodeManagerFactoryType(type)).Where(type => type != null);
            return nodeManagerFactories.ToList();
        }

        /// <summary>
        /// Helper to determine the INodeManagerFactory by reflection.
        /// </summary>
        private static INodeManagerFactory IsINodeManagerFactoryType(Type type)
        {
            var nodeManagerTypeInfo = type.GetTypeInfo();
            if (nodeManagerTypeInfo.IsAbstract ||
                !typeof(INodeManagerFactory).IsAssignableFrom(type))
            {
                return null;
            }
            return Activator.CreateInstance(type) as INodeManagerFactory;
        }
        static async Task<string> getTokenAsync()
        {
            string url = siteUrl + "/api/TokenAuth/login";
            var payload = new
            {
                data = "n3wtM/9anoNy63yRjBnxGP/1vSedgobvJiJlGJ65qWtxk+G+FvNMUCeypfR3++55vjquF9d0MLizoMxSEWIGjjHlr90bAMQQtJwzP7v1NwumyWBvxNtCPJqe3eQuT7QOMsQUx+UEyLVedP5FdJl/en4Iv4ONgD4jdwnYf7tokDM=,kVaAy+BC6Wa0mZMjaHu8Owq/+Cv0+lIJFz0ueOR+b04XsvKzIiXLZZoHMs1mio0lHWLh5NKzVLyPW7xg6jkNVKbeCyi2o1bz4+mT/lwEjxmDhu6/tvAYBO1d7QneUJBkGtnO+pu2sCSoJQdEMCyWMfYpN+aZW3HgmJA+XN/qyeE="
            };
            string response = await PostJsonAsync(url, payload);
            if (response == null)
            {
                Console.WriteLine("token获取失败！");
                return null;
            }
            ResponseData responseJson = JsonSerializer.Deserialize<ResponseData>(response);

            string[] arr = responseJson.data.Split(',');
            response = "";
            foreach (string item in arr)
            {
                response += decode(item).Replace("\r\n", ""); ;
            }
            AuthData AuthInfo = JsonSerializer.Deserialize<AuthData>(response);
            return AuthInfo.access_token;
        }
        static string decode(string input)
        {
            string privateKeyPem = "MIICXAIBAAKBgQC4nXnqUmuwlfST/7Mf6SkAkObEW8Rqf1Fu7Kh40A9OwUxKhjcvsDKfTwb18EanoM9+fBfOgrMs2AWwOgbePMRxGZ3Erwo9DqsC/ap4TyPeBLluJH8Zqu3vD++WjfHw+QbMEXFcxelINVczVbJ0qmjdOogJc3+w2/cZSp6Ud7xK8wIDAQABAoGAGoe232+cvjGuhh421Z0iIUyxfQJbBZrqTvB/fW0Y5g5tMkB7acT+YVpv+6Pd43T+nISkvy6VJRqeJqcQGZvN9txViN2USSWa68dWkADdxZukVIfeuRA8zIYprbn6SitlAvqsMr8lEuTwhXOwfF2zZ3ZiSV8Q/ZaXamvpEak6viECQQDjcmpDtWvlaTE6IlzEz8XDCZ1jORwZ40Kb96+0ZKjDB6AUIyGfpFBsDaomE/pE6EnkYqOj5xrJMq4ZcGckLeyJAkEAz8qNfbq60g4wuwqnRXSXbgyqGMPtLUmbP3J+3GDc2cNqaGzMpTbVDB3RDKoqNF0LUtYyLscMtPZMHT4eCsB0mwJBAMx6pWxf4zOpQZd5IxvRi4LP1w5IXqordUvQ/sbYJBzKczEHcIgcaizqkAiRt1NR3nST9Xg6Igu2I209b4zIOLkCQEIQz53LEf0ZX+sIPxi5MjBePHK1UKKWhZLNr4IYFf/yvtFGzmY1IdXBYScar2KItsH2smvnA6ZBrP+bMWgPusMCQDiVBuwlLrCJD2bCjVBP3Qq5Kr/peP/Oo/fmcTI1I0nNSJwkA79Bewnf7cjMUulQMjgLcioG7Mkcdc/ru6nqyxc=";
            byte[] privateKeyBytes = Convert.FromBase64String(privateKeyPem);
            RSA rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            string base64InputData = input;
            byte[] encryptedData = Convert.FromBase64String(base64InputData);
            byte[] decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);
            string outputData = Encoding.UTF8.GetString(decryptedData);
            return outputData;
        }

        static async Task<string> GetAsync(string url, string param, AuthenticationHeaderValue? authentication = null)
        {
            using var client = new HttpClient();
            if (authentication != null)
            {
                client.DefaultRequestHeaders.Authorization = authentication;
            }
            url += param;
            try
            {
                var response = await client.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"http响应错误，{e.Message}");
                return null;
            }
        }

        static async Task<string> PostJsonAsync(string url, object payload, AuthenticationHeaderValue? authentication = null)
        {
            using var client = new HttpClient();
            if (authentication != null)
            {
                client.DefaultRequestHeaders.Authorization = authentication;
            }
            var payloadJson = JsonSerializer.Serialize(payload);
            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync(url, content);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"http响应错误，{e.Message}");
                return null;
            }
        }

        #region Private Members
        public class ResponseData
        {
            public string? data { get; set; }
            public bool? success { get; set; }
        }
        public class AuthData
        {
            public string? access_token { get; set; }
            public string? refresh_token { get; set; }
            public string? scope { get; set; }
            public string? token_type { get; set; }
            public int? expires_in { get; set; }
        }
        public static string token = "";
        public const string siteUrl = "http://10.110.2.157:31000";//"http://192.168.1.1:31000";
        private static IList<INodeManagerFactory> m_nodeManagerFactories;
        #endregion
    }
}
