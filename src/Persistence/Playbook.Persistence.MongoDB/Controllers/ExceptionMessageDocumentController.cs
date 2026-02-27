using Microsoft.AspNetCore.Mvc;
using Playbook.Persistence.MongoDB.Application;
using Playbook.Persistence.MongoDB.Domain.Documents;

namespace Playbook.Persistence.MongoDB.Controllers;

[ApiController]
[Route("[controller]")]
public class ExceptionMessageDocumentController(IDocumentCollection documentCollection) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var documents = await documentCollection.ExceptionMessageDocuments.FindAllAsync();

        return Ok(documents);
    }

    [HttpGet("paginate")]
    public async Task<IActionResult> GetByPaginate(int page, int size)
    {
        var documents = await documentCollection.ExceptionMessageDocuments.FindAllByPaginateAsync(index: page, size: size);

        return Ok(documents);
    }

    [HttpPost]
    public async Task<IActionResult> Add(ExceptionMessageDocumentRequest request)
    {
        var exceptionMessageDocument = new ExceptionMessageDocument
        {
            Code = request.Code,
            Message = request.Message
        };

        await documentCollection.ExceptionMessageDocuments.AddAsync(exceptionMessageDocument);

        return Ok(exceptionMessageDocument);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ExceptionMessageDocumentRequest request)
    {
        var exceptionMessageDocument = await documentCollection.ExceptionMessageDocuments.FindOneAsync(x => x.Id == id);

        if (exceptionMessageDocument != null)
        {
            exceptionMessageDocument.Code = request.Code;
            exceptionMessageDocument.Message = request.Message;

            await documentCollection.ExceptionMessageDocuments.UpdateAsync(exceptionMessageDocument);
            return Ok();
        }

        return NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await documentCollection.ExceptionMessageDocuments.DeleteAsync(id);

        return Ok();
    }

    public class ExceptionMessageDocumentRequest
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
