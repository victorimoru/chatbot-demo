# Use Node.js base image
FROM node:18-alpine

# Install Azure SWA CLI globally
RUN npm install -g @azure/static-web-apps-cli

# Set working directory
WORKDIR /app

# Copy published files
COPY ./publish/wwwroot /app

# Expose default SWA port
EXPOSE 4280

# Start the server
CMD ["swa", "start", "/app", "--port", "4280"]