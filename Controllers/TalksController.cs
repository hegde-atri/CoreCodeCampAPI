using System;
using System.Threading.Tasks;
using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace CoreCodeCamp.Controllers
{
  [ApiController]
  [Route("api/camps/{moniker}/talks")]
  public class TalksController : ControllerBase
  {
    private readonly ICampRepository _repository;
    private readonly IMapper _mapper;
    private readonly LinkGenerator _linkGenerator;
    private readonly ILogger<TalksController> _logger;

    public TalksController(ICampRepository repository, IMapper mapper, LinkGenerator linkGenerator, ILogger<TalksController> logger)
    {
      _repository = repository;
      _mapper = mapper;
      _linkGenerator = linkGenerator;
      _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<TalkModel[]>> Get(string moniker)
    {
      try
      {
        var talks = await _repository.GetTalksByMonikerAsync(moniker, true);
        return _mapper.Map<TalkModel[]>(talks);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex.ToString());
        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
      }
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TalkModel>> Get(string moniker, int id)
    {
      try
      {
        var talk = await _repository.GetTalkByMonikerAsync(moniker, id, true);
        if (talk == null) return NotFound();
        return _mapper.Map<TalkModel>(talk);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex.ToString());
        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
      }
    }

    [HttpPost]
    public async Task<ActionResult<TalkModel>> Post(string moniker, TalkModel model)
    {
      try
      {
        var camp = await _repository.GetCampAsync(moniker);
        if (camp == null) return BadRequest("Camp does not exist");

        var talk = _mapper.Map<Talk>(model);
        talk.Camp = camp;

        if (model.Speaker == null) return BadRequest("Speaker not specified");
        var speaker = await _repository.GetSpeakerAsync(model.Speaker.SpeakerId);
        if (speaker == null) return BadRequest("Speaker not found");
        talk.Speaker = speaker;
        
        _repository.Add(talk);
        if (await _repository.SaveChangesAsync())
        {
          var url = _linkGenerator.GetPathByAction(HttpContext,
            "Get",
            values: new { moniker, id = talk.TalkId});
          return Created(url, _mapper.Map<TalkModel>(talk));
        }
        else
        {
          return BadRequest("Failed to save new Talk");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex.ToString());
        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
      }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TalkModel>> Put(string moniker, int id, TalkModel model)
    {
      try
      {
        var talk = await _repository.GetTalkByMonikerAsync(moniker, id, true);
        if (talk == null) return NotFound("Talk not found");

        if (model.Speaker != null)
        {
          var speaker = await _repository.GetSpeakerAsync(model.Speaker.SpeakerId);
          if (speaker != null)
          {
            talk.Speaker = speaker;
          }
        }

        _mapper.Map(model, talk);
        if (await _repository.SaveChangesAsync())
        {
          return _mapper.Map<TalkModel>(talk);
        }
        else
        {
          return BadRequest("Failed to update database");
        }

      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        _logger.LogError(e.ToString());
        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
      }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(string moniker, int id)
    {
      try
      {
        var talk = await _repository.GetTalkByMonikerAsync(moniker, id);
        if (talk == null) return NotFound();
        _repository.Delete(talk);
        if (await _repository.SaveChangesAsync())
        {
          return Ok();
        }

        return BadRequest("Failed to delete");

      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        _logger.LogError(e.ToString());
        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get talks");
      }
    }
    
  }
}