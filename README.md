# EvaluateHomeWork

## Description
EvaluateHomeWork is a .NET console application that uses AI to automatically review and assess student-written texts. The evaluation is performed according to a rubric provided by the teacher, allowing for flexible and criteria-based feedback. The application connects to Google Drive, retrieves Google Docs from a specified folder, and generates a structured JSON report with the evaluation results for each document.

## Features
- Connects to Google Drive and Google Docs using OAuth 2.0.
- Uses OpenAI (ChatGPT) to evaluate student texts.
- Customizable evaluation criteria via teacher-provided rubric (JSON format).
- Outputs results in a structured, easy-to-read JSON file.

## Example Rubric (in English)
Below is an example of a rubric file (JSON) that the teacher must provide. Each criterion includes a name, description, and the possible levels:

```json
{
  "Evaluation": [
    {
      "Criterion": "Spelling",
      "Description": "Correct use of spelling and punctuation.",
      "Levels": [
        { "Level": 1, "Description": "Many spelling errors." },
        { "Level": 2, "Description": "Some spelling errors." },
        { "Level": 3, "Description": "Few spelling errors." },
        { "Level": 4, "Description": "No spelling errors." }
      ]
    },
    {
      "Criterion": "Cohesion",
      "Description": "Logical connection of ideas and paragraphs.",
      "Levels": [
        { "Level": 1, "Description": "No logical connection between ideas." },
        { "Level": 2, "Description": "Some ideas are connected." },
        { "Level": 3, "Description": "Most ideas are well connected." },
        { "Level": 4, "Description": "All ideas are logically connected." }
      ]
    }
  ]
}
```

## Usage

### Prerequisites
- .NET 8.0 Runtime
- Google API credentials (OAuth 2.0 Client ID and Secret)
- OpenAI API key

### Command Line
Run the program with the following parameters:

```
dotnet run -- -f <GoogleFolderName> -r <rubricFile>
```

- `-f <GoogleFolderName>`: Name of the Google Drive folder containing the student documents.
- `-r <rubricFile>`: Path to the rubric JSON file (created by the teacher).
- `-h`: Show help and usage information.

### Example
```
dotnet run -- -f "StudentEssays" -r "rubric.json"
```

### Output
- The program will generate a JSON file named after the folder, containing the evaluation results for each document.

## License
MIT License. See LICENSE file for details.
