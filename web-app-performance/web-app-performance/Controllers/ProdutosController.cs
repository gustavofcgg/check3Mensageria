using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Text;
using System.Threading.Channels;
using web_app_domain;
using web_app_repository;
using static System.Net.Mime.MediaTypeNames;


namespace web_app_performance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutosController : ControllerBase
    {
        private static ConnectionMultiplexer redis;
        private readonly IProdutosRepository _repository;


        //onde ira receber a injeção
        public ProdutosController(IProdutosRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetProdutos()
        {
            string key = "getprodutos";
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(10));
            string produto = await db.StringGetAsync(key);

            if (!string.IsNullOrEmpty(produto))
            {
                return Ok(produto);
            }

            var produtos = await _repository.ListarProdutos();
            string produtosJson = JsonConvert.SerializeObject(produtos);
            await db.StringSetAsync(key, produtosJson);
            return Ok(produtos);

        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Produtos produtos)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "estoque",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete:
                                     false,
                                     arguments: null);

                var mensagem = new
                {
                    produtoId = produtos.Id, 
                    nomeProduto = produtos.Nome, 
                    quantidadeInicial = produtos.Quant_estoque
                };


                string mensagemJson = JsonConvert.SerializeObject(mensagem);
                var body = Encoding.UTF8.GetBytes(mensagemJson);

                channel.BasicPublish(exchange: "",
                                     routingKey: "fila_teste",
                                     basicProperties: null,
                                     body: body);

                Console.WriteLine($"Enviado");
            }

                await _repository.SalvarProdutos(produtos);

            //apagar o cache
            //conexao c redis
            string key = "getprodutos";
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);
            return Ok();

        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Produtos produtos)
        {
            await _repository.AtualizarProdutos(produtos);

            string key = "getprodutos";
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);
            return Ok();

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repository.RemoverProdutos(id);

            string key = "getprodutos";
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            await db.KeyDeleteAsync(key);

            return Ok();

        }
    }
}
