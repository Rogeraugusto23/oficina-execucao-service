using ExecucaoService.Application.Common;
using ExecucaoService.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExecucaoService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExecucoesController : ControllerBase
{
    private readonly ConsultarExecucaoUseCase _consultarExecucao;
    private readonly IniciarExecucaoUseCase _iniciarExecucao;
    private readonly FinalizarExecucaoUseCase _finalizarExecucao;

    public ExecucoesController(
        ConsultarExecucaoUseCase consultarExecucao,
        IniciarExecucaoUseCase iniciarExecucao,
        FinalizarExecucaoUseCase finalizarExecucao)
    {
        _consultarExecucao = consultarExecucao;
        _iniciarExecucao = iniciarExecucao;
        _finalizarExecucao = finalizarExecucao;
    }

    // GET: api/Execucoes/ordem-servico/{ordemServicoId}
    [HttpGet("ordem-servico/{ordemServicoId}")]
    public async Task<IActionResult> GetPorOrdemServico(Guid ordemServicoId)
    {
        var resultado = await _consultarExecucao.ExecutarPorOrdemServicoAsync(ordemServicoId);
        return TraduzirResultado(resultado);
    }

    // POST: api/Execucoes/{id}/iniciar
    // Acionado pelo mecânico ao começar a trabalhar na OS.
    [HttpPost("{id}/iniciar")]
    public async Task<IActionResult> Iniciar(Guid id)
    {
        var resultado = await _iniciarExecucao.ExecutarAsync(id);
        return TraduzirResultado(resultado);
    }

    // POST: api/Execucoes/{id}/finalizar
    // Acionado pelo mecânico ao concluir (ou falhar) o serviço — dispara
    // ExecucaoFinalizadaEvent para o OS Service (ver FinalizarExecucaoUseCase).
    [HttpPost("{id}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid id, [FromBody] FinalizarExecucaoInput input)
    {
        var resultado = await _finalizarExecucao.ExecutarAsync(id, input.Sucesso, input.MotivoFalha);
        return TraduzirResultado(resultado);
    }

    private IActionResult TraduzirResultado<T>(OperationResult<T> resultado)
    {
        if (resultado.Sucesso)
            return Ok(resultado.Dados);

        return resultado.Erro switch
        {
            TipoErro.NaoEncontrado => NotFound(resultado.Mensagem),
            _ => BadRequest(resultado.Mensagem)
        };
    }

    public class FinalizarExecucaoInput
    {
        public bool Sucesso { get; set; }
        public string? MotivoFalha { get; set; }
    }
}
