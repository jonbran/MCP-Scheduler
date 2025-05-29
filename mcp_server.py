from mcp.server.fastmcp import FastMCP
from datetime import datetime

# Create an MCP server
mcp = FastMCP("McpScheduler")

# In-memory storage for scheduled conversations
conversations = {}

@mcp.tool()
def schedule_conversation(conversation_text: str, scheduled_time: str, endpoint: str, method: str = "POST", additional_info: str = None) -> str:
    """Schedule a conversation for future delivery."""
    if not conversation_text or not scheduled_time or not endpoint:
        raise ValueError("conversation_text, scheduled_time, and endpoint are required")

    try:
        scheduled_datetime = datetime.fromisoformat(scheduled_time)
        if scheduled_datetime <= datetime.utcnow():
            raise ValueError("Scheduled time must be in the future")
    except ValueError:
        raise ValueError("Invalid scheduled_time format. Use ISO 8601 format.")

    conversation_id = str(len(conversations) + 1)
    conversations[conversation_id] = {
        "text": conversation_text,
        "time": scheduled_datetime,
        "endpoint": endpoint,
        "method": method,
        "info": additional_info,
        "status": "scheduled"
    }
    return conversation_id

@mcp.tool()
def get_conversation_status(conversation_id: str) -> str:
    """Get the status of a scheduled conversation."""
    if conversation_id not in conversations:
        raise ValueError("Conversation ID not found")
    return conversations[conversation_id]["status"]

@mcp.tool()
def cancel_conversation(conversation_id: str) -> bool:
    """Cancel a scheduled conversation."""
    if conversation_id not in conversations:
        raise ValueError("Conversation ID not found")
    conversations[conversation_id]["status"] = "cancelled"
    return True

if __name__ == "__main__":
    mcp.run()
