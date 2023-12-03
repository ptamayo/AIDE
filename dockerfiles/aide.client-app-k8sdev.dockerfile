FROM node:14.16.1-alpine as build

WORKDIR /app

COPY Aide.ClientApp/package.json .

RUN npm install

COPY . .

# Dev notes: This script configuration it's been added in package.json
RUN npm run build:k8sdev --prefix=Aide.ClientApp

FROM nginx:stable-alpine

# Update nginx to allow page refresh for angular.
# Reference: https://stackoverflow.com/questions/56213079/404-error-on-page-refresh-with-angular-7-nginx-and-docker
COPY Aide.ClientApp/nginx/k8s/default.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/Aide.ClientApp/dist/bootstrap-template /usr/share/nginx/html

EXPOSE 80

CMD [ "nginx", "-g", "daemon off;" ]