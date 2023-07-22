using FormulaCEE.API.Models;
using FormulaCEE.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormulaCEE.API.Controllers;

[ApiController]
[Route("[controller]")]
public class SolicitacoesController : ControllerBase
{
    private readonly ILogger<SolicitacoesController> _logger;
    private readonly IMessageProducer _messageProducer;

    public static readonly List<Solicitacao> _solicitacoes = new();

    public SolicitacoesController(
        ILogger<SolicitacoesController> logger,
        IMessageProducer messageProducer)
    {
        _logger = logger;
        _messageProducer = messageProducer;
    }

    [HttpPost]
    public IActionResult CreatingSolicitacao(Solicitacao newSolicitacao)
    {
        if(!ModelState.IsValid) return BadRequest();

        _solicitacoes.Add(newSolicitacao);

        _messageProducer.EnviarMensagem<Solicitacao>(newSolicitacao);

        return Ok();
    }

    private bool ValidarSenha(Solicitacao newSolicitacao)
    {
        var dataNascimento = newSolicitacao.DataNasc.Value;
        var ano = dataNascimento.Year.ToString().Substring(2);
        var mes = dataNascimento.Month.ToString().PadLeft(2, '0');
        var dia = dataNascimento.Day.ToString().PadLeft(2, '0');

        var idade = DateTime.Today.Year - dataNascimento.Year;
        var senha = newSolicitacao.Senha.ToString();
        var senhaConfirm = newSolicitacao.SenhaConfirm.ToString();

        if (idade >= 18 && senha.Length == 6 && senha != (ano + mes + dia) && int.TryParse(senha, out int senhaNumerica) && senha.Distinct().Count() == 6)
        {
            bool possuiSequencia = false;
            for (int i = 0; i < senha.Length - 1; i++)
            {
                if (senha[i] + 1 == senha[i + 1])
                {
                    possuiSequencia = true;
                    break;
                }
            }
            return !possuiSequencia;
        }

        return false;
    }

    private string GerarNumeroCartao()
    {
        var rnd = new Random();

        var prefixo = rnd.Next(1000, 9999).ToString();

        var primeirosnumerosMeio = "";
        for (var i = 0; i < 4; i++)
        {
            var grupo = rnd.Next(1000, 9999);
            primeirosnumerosMeio = grupo.ToString() + " ";
        }

        var ultimossnumerosMeio = "";
        for (var i = 0; i < 4; i++)
        {
            var grupo2 = rnd.Next(1000, 9999);
            ultimossnumerosMeio = grupo2.ToString() + " ";
        }

        var ultimosDigitos = rnd.Next(1000, 9999).ToString();

        var numeroCartao = prefixo + " " + primeirosnumerosMeio + ultimossnumerosMeio + ultimosDigitos;
        return numeroCartao;
    }

    [HttpPost("solicitar")]
    public async Task<ActionResult<Solicitacao>> Solicitar([FromBody] Solicitacao solicitacao)
    {
        Random rnd = new Random();
        var checarSenha = ValidarSenha(solicitacao);

        if (DateTime.Today.Year - solicitacao.DataNasc.Value.Year < 18)
            return BadRequest("É obrigatório ter 18 ou mais de idade para solicitar um cartão.");

        if (solicitacao.Bandeira != "Mastercard" && solicitacao.Bandeira != "Visa")
        {
            return BadRequest("A bandeira do cartão deve ser: 'Mastercard' ou 'Visa'.");
        }

        if (solicitacao.DataVenc != "5" && solicitacao.DataVenc != "10" && solicitacao.DataVenc != "15" && solicitacao.DataVenc != "20")
        {
            return BadRequest("A Data de Vencimento deve ser: '5', '10', '15', ou '20'.");
        }

        if (solicitacao.Tipo != "PLATINUM" && solicitacao.Tipo != "GOLD" && solicitacao.Tipo != "BLACK" && solicitacao.Tipo != "DIAMOND")
        {
            return BadRequest("O Tipo do cartão deve ser: 'PLATINUM', 'GOLD', 'BLACK' ou 'DIAMOND'.");
        }
        else
        {
            switch (solicitacao.Tipo)
            {
                case "GOLD":
                    solicitacao.Limite = "R$1.500,00";
                    break;
                case "PLATINUM":
                    solicitacao.Limite = "R$15.000,00";
                    break;
                case "BLACK":
                    solicitacao.Limite = "R$30.000,00";
                    break;
                case "DIAMOND":
                    solicitacao.Limite = "ILIMITADO";
                    break;
            }
        }

        if (!checarSenha)
        {
            return BadRequest("Por favor, insira uma senha de 6 dígitos que cumpra com os seguintes requisitos:" +
                                "\n1. Não corresponda a sua data de nascimento" +
                                "\n2. Não possua números repetidos" +
                                "\n3. Não possua números em sequencia.");
        }
        else
        {
            //solicitacao.Status = "ENTREGUE"
            solicitacao.Cvv = rnd.Next(100, 1000).ToString();
            solicitacao.NumeroCartao = GerarNumeroCartao();

            try
            {
                if(!ModelState.IsValid) return BadRequest();

                _solicitacoes.Add(solicitacao);

                _messageProducer.EnviarMensagem<Solicitacao>(solicitacao);

                return Ok("ID do seu Cartão: " + solicitacao.Id + "\n" + 
                            "Número do seu Cartão: " + solicitacao.NumeroCartao + "\n" + 
                            "Nome a ser impresso: " + solicitacao.NomeCartao + "\n" + 
                            "Data de Vencimento: " + solicitacao.DataVenc + " anos" + "\n" + 
                            "\nPara ativar o seu cartão, realize as seguintes tarefas:\n" +
                            "Utilize o serviço 'Entregar' e insira os seguintes dados: " +
                            "Id, Número do cartão, Agência, Conta e senha --> " + solicitacao.Senha +
                            "\nUtilize o serviço 'Ativar' e insira os seguintes dados: " +
                            "Id, Número do cartão, Agência, Conta e Senha --> " + solicitacao.Senha);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao solicitar o cartão.");

            }
        }
    }
}
