
- build image
    ```bash
    docker buildx build -f Dockerfile . --platform linux/amd64,linux/arm64 -t yowko/redismemoryusage --push 
    ```

- analysis

    ```
    docker run --rm -e endpoint="192.168.80.3:7002" yowko/redismemoryusage
    ```