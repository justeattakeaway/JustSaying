# Documentation Generation

## PlantUML

> PlantUML is used to draw UML diagrams, using a simple and human readable text description. [plantuml.com](https://plantuml.com/)

## Integration with Visual Studio Code

- Install the plugin
    - [PlantUML plugin for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=jebbs.plantuml)

    - Add the below to your user settings in Visual Studio Code

    ```json
        "plantuml.exportFormat": "png",
        "plantuml.render": "PlantUMLServer",
        "plantuml.server": "http://localhost:8080",
        "plantuml.diagramsRoot": "docs",
        "plantuml.exportOutDir": "docs",
        "plantuml.exportSubFolder": false
    ```

- Run a [PlantUML Server](https://github.com/plantuml/plantuml-server) locally to translate from `.puml` files into `.png` images

    ```sh
        docker run -d -p 8080:8080 plantuml/plantuml-server:jetty
    ```

- To live preview an image while editing
    - Open a `.puml`
    - Press F1
    - Select `PlantUML: Preview Current Diagram`

- To regenerate all images
    - Press F1
    - Select `PlantUML: Export workspace diagrams`
    - All images in `/docs` will be regenerated

## Further reading

- Further reading and examples available at <https://www.planttext.com>
