docker run --rm -d -it --name otlp -p 4317:4317 -p 4318:4318 -v ./otel-config.yaml:/etc/otelcol/config.yaml otel/opentelemetry-collector-contrib:latest

docker logs -f otlp